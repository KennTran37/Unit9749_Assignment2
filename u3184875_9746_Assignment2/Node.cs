using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    public class Node
    {
        public string name = null;
        public NodeType nodeType;
        public Point position;

        public Node(string nodeName, NodeType type)
        {
            name = nodeName;
            nodeType = type;
            position = Form1.inst.GetNodeLocation(type);
        }
    }

    public class Site : Node, ISpace
    {
        public SiteListBox listBox;
        public int maxAgents = 5;
        public List<Agent> currentAgents;

        public Inventory inventory;

        public delegate void RefreshForm3();
        public RefreshForm3 refreshHandler;

        public Site(string name, NodeType nodeType, int maxAgents) : base(name, nodeType)
        {
            currentAgents = new List<Agent>();
            this.maxAgents = maxAgents;

            inventory = new Inventory();
        }

        public void DisplaySiteInformation(object sender, EventArgs e)
        {
            if (!Form1.inst.SiteFormAlreadyOpened(name))
            {
                Form3 form3 = new Form3(this);
                form3.Name = $"Form3_{name}";
                form3.Show();
            }
        }

        public void AddAgent(Agent agent)
        {
            currentAgents.Add(agent);
            refreshHandler?.Invoke();
        }

        public void RemoveAgent(Agent agent)
        {
            currentAgents.Remove(agent);
            refreshHandler?.Invoke();
        }

        public bool HasSpace() => currentAgents.Count < maxAgents;
        public bool HasAmount(int value) => throw new Exception("Method for Site Class cannot be used");
    }

    //holds the form elements which will display the site's infomation
    public struct SiteListBox
    {
        public GroupBox siteBox;
        public PictureBox siteIcon;
        public Label name;
        public Label workers;

        public SiteListBox(GroupBox siteBox, PictureBox siteIcon, Label name, Label workers)
        {
            this.siteBox = siteBox;
            this.siteIcon = siteIcon;
            this.name = name;
            this.workers = workers;
        }
    }

    public class Inventory
    {
        public MaterialBox wood;
        public MaterialBox plank;
        public MaterialBox ore;
        public MaterialBox ingot;

        //creating empty inventory
        public Inventory()
        {
            wood = new MaterialBox();
            plank = new MaterialBox();
            ore = new MaterialBox();
            ingot = new MaterialBox();
        }
        //assigning amount into materials
        public Inventory(int current, int max)
        {
            wood = new MaterialBox(MaterialType.Wood, current, max);
            plank = new MaterialBox(MaterialType.Plank, current, max);
            ore = new MaterialBox(MaterialType.Ore, current, max);
            ingot = new MaterialBox(MaterialType.Ingot, current, max);
        }
        //reassigning materials when the player stops the construction
        public Inventory(Inventory inventory)
        {
            wood = inventory.wood;
            plank = inventory.plank;
            ingot = inventory.ingot;
            ore = inventory.ore;
        }
    }

    //holds the information of each materials inside the inventory
    public struct MaterialBox : ISpace
    {
        public MaterialType materialType;
        public GroupBox materialBox;
        public PictureBox icon;
        public Label label;

        private int current;
        private int max;

        //used to assign site inventory
        public MaterialBox(GroupBox materialBox, PictureBox icon, Label label, MaterialType type) : this()
        {
            this.materialBox = materialBox;
            this.icon = icon;
            this.label = label;
            materialType = type;
            Max = 50;
            Current = 5;
        }

        //used to assign agent inventory
        public MaterialBox(MaterialType type, int current, int max)
        {
            materialBox = null;
            icon = null;
            label = null;
            materialType = type;
            this.current = current;
            this.max = max;
        }

        public int Max
        {
            get => max; set
            {
                max = value;
                if (label != null)
                    SetCountText();
            }
        }
        public int Current
        {
            get => current; set
            {
                current = value;
                if (label != null)
                    SetCountText();
            }
        }
        
        //after the max or current values are changed, update the label
        void SetCountText()
        {
            if (label.InvokeRequired)
                label.Invoke(new Action(SetCountText));
            else
                label.Text = $"{current}/{max}";
        }

        public bool HasSpace() => current < max;
        public bool HasAmount(int value) => current >= value;

        public bool TryPutInMaterial()
        {
            if (current < max)
            {
                Current++;
                return true;
            }
            return false;
        }

        public bool TryTakeOutMaterial()
        {
            if (current > 0)
            {
                Current--;
                return true;
            }
            return false;
        }
    }
}


//A simple interface where the methods are used to check if there is enough space or has an amount
interface ISpace
{
    bool HasAmount(int value);
    bool HasSpace();
}