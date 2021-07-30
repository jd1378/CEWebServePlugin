using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CESDK;
using CEWebServePlugin;

namespace CEWebServePlugin
{
    class WebServePlugin : CESDKPluginClass
    {
        private WebServer webServer = new WebServer();
        public override string GetPluginName()
        {
            return "WebServePlugin";
        }



        public override bool DisablePlugin() //called when disabled
        {
            return true;
        }

        public override bool EnablePlugin() //called when enabled
        {
            //you can use sdk here
            // sdk.lua.DoString("print('I am alive')");


            sdk.lua.Register("toggleserve", toggleServe);
            sdk.lua.Register("showsettings", showSettings);

            sdk.lua.DoString(@"local m=MainForm.Menu
                local topm=createMenuItem(m)
                topm.Caption='WebServe'
                m.Items.insert(MainForm.miHelp.MenuIndex,topm)

                local mf1=createMenuItem(m)
                mf1.Caption='Start Serving';
                mf1.OnClick=function(s)
                  toggleserve()
                end
                topm.add(mf1)

                function changeServeBtn(caption, enabled)
                  mf1.Caption = caption
                  mf1.Enabled = enabled
                end

                --[[
                local mf2=createMenuItem(m)
                mf2.Caption='Show Settings';
                mf2.OnClick=function(s)
                  showsettings()
                end
                topm.add(mf2)
                --]]
            ");

            return true;
        }

        public void ChangeServeBtn(string caption, Boolean isEnabled)
        {
            var l = sdk.lua;
            l.GetGlobal("changeServeBtn");

            if (l.IsFunction(-1))
            {
                l.PushString(caption);
                l.PushBoolean(isEnabled);
                l.PCall(2, 0);
            }
            else
                MessageBox.Show("failed to execute changeServeBtn");
            l.Pop(1);
        }

        int MyFunction2(IntPtr L)
        {
            var l = sdk.lua;

            l.DoString("MainForm.Caption='Changed by test2()'");

            return 2;
        }

        public void NewThreadExample()
        {
            //return;
            sdk.lua.DoString("print('Running from a different thread. And showing this requires the synchronize capability of the main thread')"); //print is threadsafe

            //now running an arbitrary method in a different thread

        }
        int MyFunction3()
        {
            Thread thr = new Thread(NewThreadExample);
            int i = 0;
            thr.Start();

            while (thr.IsAlive)
            {
                sdk.CheckSynchronize(10); //ce would freeze without this as print will call Synchronize to run it in the main thread               
                i = i + 1;
            }

            sdk.lua.PushInteger(i);

            return 1;
        }

        void NewGuiThread()
        {
            int i = sdk.lua.GetTop();

            SettingsForm formpy = new SettingsForm();

            try
            {
                System.Windows.Forms.Application.Run(formpy);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return;
        }

        int showSettings()
        {
            if (sdk.lua.ToBoolean(1))
            {
                //run in a thread
                Thread thr = new Thread(NewGuiThread);
                thr.Start();
            }
            else
            {
                //formpy.Show(); //or formpy.ShowDialog()
                //run in the current thread (kinda)
                NewGuiThread();
            }

            sdk.lua.PushInteger(100);
            return 1;
        }

        void startListening()
        {
            webServer.Start();
        }

        int toggleServe()
        {

            if (!webServer.IsServing)
            {
                Thread thr = new Thread(startListening);
                thr.Start();
                ChangeServeBtn("Stop Serving", true);

            }
            else
            {
                webServer.Stop();
                ChangeServeBtn("Start Serving", true);
            }
            return 1;
        }
    }
}
