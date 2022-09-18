using System;
using System.Windows.Forms;

namespace MissionPlanner.BonsVoosGrid 
{
    public class BonsVoosGridPlugin : MissionPlanner.Plugin.Plugin
    {



        ToolStripMenuItem but;

        public override string Name
        {
            get { return "BonsVoosGrid"; }
        }

        public override string Version
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public override string Author
        {
            get { return "Fernando Ramos"; }
        }

        public override bool Init()
        {
            return true;
        }

        public override bool Loaded()
        {
            //System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BonsVoosGridUI));
            //var temp = (string)(resources.GetObject("$this.Text"));

            //but = new ToolStripMenuItem(temp);
            //but.Click += but_Click;

            //bool hit = false;
            //ToolStripItemCollection col = Host.FPMenuMap.Items;
            //int index = col.Count;
            //foreach (ToolStripItem item in col)
            //{
            //    if (item.Text.Equals(Strings.AutoWP))
            //    {
            //        index = col.IndexOf(item);
            //        ((ToolStripMenuItem)item).DropDownItems.Add(but);
            //        hit = true;
            //        break;
            //    }
            //}

            //if (hit == false)
            //    col.Add(but);

            return true;
        }

        public void but_Click(object sender, EventArgs e)
        {
            using (var BVgridui = new BonsVoosGridUI(this))
            {
                //fernando 27-07-2022 - sem aplicar tema
                MissionPlanner.Utilities.ThemeManager.ApplyThemeTo(BVgridui);

                if (Host.FPDrawnPolygon != null && Host.FPDrawnPolygon.Points.Count > 2)
                {
                    BVgridui.ShowDialog();
                }
                else
                {
                    if (
                        CustomMessageBox.Show("Nenhum polígono definido. Carregar arquivo ?", "Load File", MessageBoxButtons.YesNo) ==
                        (int)DialogResult.Yes)
                    {
                        BVgridui.LoadBonsVoosGrid() ;
                        BVgridui.ShowDialog();
                    }
                    else
                    {
                        CustomMessageBox.Show("Defina um polígono.", "Error");
                    }
                }
            }
        }

        public override bool Exit()
        {
            return true;
        }



    }
}
