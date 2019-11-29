// Copyright 2018-2019 Fabulous contributors. See LICENSE.md for license.
namespace CounterApp

open System.Collections.Generic
open System
open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.LiveUpdate
open Xamarin.Forms
open System.Diagnostics

module App =
    [<Literal>]
    let UserCount=100
    let inline startOnPool (routine : Async<'unit>) = async {
      do! Async.SwitchToThreadPool()
      return! routine
      }
    
    let mutable launchActivity = fun (s:string)->()
    type User={Id:int;Name:string;UpdateCount:int}
    
    module User=
        type private Msg =
          | Update of User
          | Get of int*AsyncReplyChannel<User>
          | All of AsyncReplyChannel<User seq>
        
        
        let cache=Dictionary<int,ViewElement>()
        let updateCount user = { user with UpdateCount = user.UpdateCount+1 }
        let generate count =
            let res = Seq.init count (fun id-> {Id=id;Name=sprintf "Пользователь %i" id;UpdateCount = 0})
            res |> Seq.map (fun u-> u.Id,u) |> Map.ofSeq

        
       
        let setName name user = {user with Name=name}
        
        let draw (user:User) =
         dependsOn (user.Id,user.Name,user.UpdateCount)
          (fun user (id,name,count) ->
            View.TextCell (text=sprintf "%s with id %i updated %i" name id count)
          )
         
        let private inbox = MailboxProcessor.Start (fun b ->
            let rec loop (state:Map<int,User>) = startOnPool <| async{
                match! b.Receive() with
                | Update user ->
                    let view = draw user
                    cache.[user.Id]<-view
                    return! loop (state.Add(user.Id,user))
                | Get (id,ch) -> ch.Reply state.[id]
                | All ch -> ch.Reply (
                                      state
                                      |> Map.toSeq
                                      |> Seq.map snd
                                      |> Seq.sortBy (fun u -> u.Id)
                                     )
               
                return! loop state
            }
            loop  (generate UserCount)
            
            ) 
        let getOrCreate user =
          match cache.TryGetValue user.Id with
          | true, view -> view
          | _ ->
              let view = draw user
              cache.[user.Id]<-view
              view
              
        let updateCache user =
            cache.[user.Id] <- draw user
            
        
        let get id = inbox.PostAndReply (fun ch-> Get (id,ch))
        
        let update user = inbox.Post (Update user)
        
        let all()=inbox.PostAndReply (fun ch-> All ch)
                              
            
            
    type Page={Users:ResizeArray<User>}
    
    module Page =
        let addUser user page= page.Users.Add user
        let mutable value = {Users = ResizeArray(User.all())}
        
        let draw (page:Page) =
            View.ContentPage
             (
                content=
                           View.StackLayout(
                                               children=
                                                 [
                                                     View.ListView
                                                       (
                                                         items = (User.cache |> Seq.sortBy (fun x->x.Key)|> Seq.map (fun x->x.Value)|>Seq.toList)
                                                       )        
                                                 ]
                                           )               
             )
        
    type Message =
    | ChangeUser of User
    | DrawPage of Page

   
    type Model = {View:ViewElement}
 

    let init () =
       {
        View = View.ContentPage
                   (
                    
                    content =
                     View.StackLayout(
                       children=
                            [
                             View.Button (text="Show",command = fun ()-> launchActivity "")
                             View.Label(
                                 text="No content",
                                 fontSize= FontSize 37.)
                            ]
                    )
                   )
        
       }, Cmd.none
    
    
    

    let update msg model =
       match msg with
       | ChangeUser user ->
           printfn "User %i updated with value %A" user.Id user
           User.update user
           model,Cmd.none
       | DrawPage page->
           printfn "Page is drawn"
           {model with View=Page.draw page},Cmd.none

    let view (model: Model) dispatch =  
       model.View
      
    let timer = new System.Timers.Timer(10.)
    let pageTimer=new System.Timers.Timer(1000.)
    
    pageTimer.AutoReset<-true
    timer.AutoReset<-true
    
    do timer.Start()
    do pageTimer.Start()
    
    let updatePage dispatch =
        pageTimer.Elapsed |> Observable.subscribe (fun e-> dispatch  (DrawPage Page.value)) |> ignore
      
    let random = System.Random(DateTime.UtcNow.Ticks|>int)  
    let updateUser() =     
        timer.Elapsed
          |> Observable.subscribe
             (fun e->
               let rnd = random.Next(0,UserCount-1)
               let user=(User.get rnd |> User.updateCount)
               User.update user) |> ignore
    
    let program = 
        Program.mkProgram init update view
        |>Program.withSubscription (fun _ -> Cmd.ofSub updatePage)
        |>Program.withSubscription (fun _ -> updateUser()
                                             Cmd.none
                                    )
     
    type Model2={Name:string}
    
    type Msg2 =
     |Name
    
    
    let init2()= {Name="Initial"},Cmd.none
    
    let updateName dispatch =
        pageTimer.Elapsed |> Observable.subscribe (fun e-> dispatch  (Name)) |> ignore
        
    let update2 msg (model:Model2)  =
        match msg with
        | Name -> {model with Name = Guid.NewGuid().ToString()},Cmd.none
        
    let view2 (model:Model2) dispatch =
        View.ContentPage(
            content=
                 View.StackLayout(
                       children=
                        [
                         View.Button (text="Show",command = fun ()-> launchActivity "")
                         View.Label (
                            horizontalOptions =LayoutOptions.Center,
                            verticalOptions = LayoutOptions.Center,
                            text=model.Name,
                            fontSize = FontSize 37.
                            
                            )
                        ])
            )
    let program2 = 
        Program.mkProgram init2 update2 view2
                                             


type CounterApp () as app = 
     inherit Application ()

     let runner =
        App.program
        |> Program.withConsoleTrace
        |> XamarinFormsProgram.run app
        
type CounterApp2 () as app = 
     inherit Application ()

     let runner =
        App.program2
        |> Program.withConsoleTrace
        |> Program.withSubscription (fun _ -> Cmd.ofSub App.updateName) 
        |> XamarinFormsProgram.run app

[<RequireQualifiedAccess>]
module Apps=       
 let app = lazy CounterApp()
 let app2 = lazy CounterApp2()





