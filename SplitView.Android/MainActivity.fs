// Copyright 2018 Fabulous contributors. See LICENSE.md for license.
namespace Droid


open Android.App
open Android.Content.PM
open Android.Content
open Android.OS
open Xamarin.Forms.Platform.Android


  

[<Activity (Label = "CounterApp.App", Icon = "@mipmap/icon", LaunchMode=LaunchMode.SingleTask,Theme = "@style/MainTheme", ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type ChildActivity() =
    inherit FormsApplicationActivity()
    override this.OnCreate (bundle: Bundle) =
        
        base.OnCreate (bundle)
        this.LoadApplication (CounterApp.Apps.app.Value)
       
        
        
[<Activity (Label = "CounterApp.Droid", Icon = "@mipmap/icon", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity() =
    inherit FormsApplicationActivity()
    override this.OnCreate (bundle: Bundle) =
        base.OnCreate (bundle)
        Xamarin.Forms.Forms.Init (this, bundle)
        this.LoadApplication (CounterApp.CounterApp2())
        
[<Application>]
type MainApplication(javaReference, transfer) =
  inherit Application(javaReference,  transfer)
  override x.OnCreate()=
    CounterApp.App.launchActivity <- fun _ ->
      let activity = new Intent (x, typeof<ChildActivity>)
      activity.AddFlags (ActivityFlags.NewTask) |> ignore
      x.StartActivity(activity)
    base.OnCreate()
        



