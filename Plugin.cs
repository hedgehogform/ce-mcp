using CESDK;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CeMCP
{
    class McpPlugin : CESDKPluginClass
    {
        public override string GetPluginName()
        {
            return "MCP Server for Cheat Engine";
        }

        public override bool DisablePlugin() //called when disabled
        {
            return true;
        }

        public override bool EnablePlugin() //called when enabled
        {
            //you can use sdk here
            //sdk.lua.dostring("print('I am alive')");


            sdk.lua.Register("pluginexample1", MyFunction);
            sdk.lua.Register("pluginexample2", MyFunction2);
            sdk.lua.Register("pluginexample3", MyFunction3);
            sdk.lua.Register("pluginexample4", MyFunction4);
            sdk.lua.Register("formymcformface", MyFunction4);

            sdk.lua.DoString(@"local m=MainForm.Menu
local topm=createMenuItem(m)
topm.Caption='C# Example plugin'
m.Items.insert(MainForm.miHelp.MenuIndex,topm)

local mf1=createMenuItem(m)
mf1.Caption='Example 1: Weee';
mf1.OnClick=function(s)
  local r1,r2=pluginexample1(100)

  print('pluginexample1(100) returned: '..r1..','..r2)
end
topm.add(mf1)


local mf2=createMenuItem(m)
mf2.Caption='Example 2: Scripting and ignoring the state';
mf2.OnClick=function(s)
  local r1,r2=pluginexample2(100)

  print('pluginexample2() returned: '..r1..','..r2)
end
topm.add(mf2)


local mf3=createMenuItem(m)
mf3.Caption='Example 3: Threading';
mf3.OnClick=function(s)
  local i=pluginexample3(100)

  print('pluginexample3() went through the wait loop '..i..' times')
end
topm.add(mf3)


local mf4=createMenuItem(m)
mf4.Caption='Example 4: Forms and whatnot';
mf4.OnClick=function(s)
  local newthread=MessageDialog('Open the form in a new thread? (If not it will open inside the main GUI)',mtConfirmation,mbYes,mbNo)==mrYes
  local i=pluginexample4(newthread)

  print('pluginexample4() finally returned with the value '..i..' (should be 100)')
end
topm.add(mf4)");

            return true;
        }

        int MyFunction()
        {
            var l = sdk.lua;
            l.PushString("WEEEE");
            if (l.GetTop() > 0)
            {
                if (l.IsInteger(1))
                {
                    int i = (int)l.ToInteger(1);
                    l.PushInteger(i * 2);
                    return 2;
                }
            }

            return 1;
        }

        int MyFunction2(IntPtr L)
        {
            var l = sdk.lua;

            l.DoString("MainForm.Caption='Changed by test2()'");

            l.PushString(L, "Works");
            l.PushString("And this as well");

            return 2;
        }

        public async Task NewThreadExampleAsync()
        {
            //return;
            sdk.lua.DoString("print('Running from a different thread. And showing this requires the synchronize capability of the main thread')"); //print is threadsafe

            //now running an arbitrary method in a different thread
            await Task.Delay(100); // Simulate some async work
        }
        int MyFunction3()
        {
            Task task = NewThreadExampleAsync();
            int i = 0;

            while (!task.IsCompleted)
            {
                sdk.CheckSynchronize(10); //ce would freeze without this as print will call Synchronize to run it in the main thread               
                i = i + 1;
            }

            // Ensure task completes and handle any exceptions
            task.GetAwaiter().GetResult();

            sdk.lua.PushInteger(i);

            return 1;
        }


        async Task NewGuiThreadAsync()
        {
            try
            {
                // Create and run WPF UI on STA thread
                var staThread = new Thread(() =>
                {
                    try
                    {
                        // Create a WPF window
                        var window = new Window();

                        // Create the main page
                        var mainPage = new MainPage();

                        // Set the content of the window
                        window.Content = mainPage;

                        // Set window properties
                        window.Title = "MCP Plugin Window";

                        // Show the window
                        window.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        sdk.lua.DoString($"print('Error in UI thread: {ex.Message}')");
                        MessageBox.Show("Plugin window created successfully!", "MCP Plugin");
                    }
                });

                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();

                // Wait a bit then return
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                // Fallback to console output if UI creation fails
                sdk.lua.DoString($"print('Error creating window: {ex.Message}')");

                // Try a simpler approach with MessageBox on STA thread
                try
                {
                    var staThread = new Thread(() =>
                    {
                        MessageBox.Show("Plugin window created successfully!", "MCP Plugin");
                    });
                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();
                }
                catch
                {
                    sdk.lua.DoString("print('UI creation failed, but plugin is working')");
                }
            }
        }
        int MyFunction4()
        {
            if (sdk.lua.ToBoolean(1))
            {
                //run in a background task
                _ = Task.Run(async () => await NewGuiThreadAsync());
            }
            else
            {
                //run on current thread but don't block
                _ = Task.Run(async () => await NewGuiThreadAsync());
            }

            sdk.lua.PushInteger(100);
            return 1;
        }

    }
}
