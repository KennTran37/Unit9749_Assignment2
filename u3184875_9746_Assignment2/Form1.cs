using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    public partial class Form1 : Form
    {
        public static Form1 inst;

        Node[] nodeMap = new Node[]
        {
           new Node("Resident Street", NodeType.ResidentStreetNode),
           new Node("Forest Center", NodeType.ForestCenterNode),
           new Node("Forest North", NodeType.ForestNorthNode),
           new Node("Forest South", NodeType.ForestSouthNode),
           new Node("Inter Street", NodeType.InterStreetNode),
           new Node("Lux Street", NodeType.LuxStreetNode),
           new Node("North Gate", NodeType.NorthGateNode),
           new Node("South Gate", NodeType.SouthGateNode),
           new Node("West Gate", NodeType.WestCornerNode)
        };
        Site[] siteMap = new Site[]
         {
           new Site("Main Site", NodeType.MainSite, 5),
           new Site("Mine Site", NodeType.MiningSite, 5),
           new Site("Forest Site", NodeType.ForestSite, 5),
           new Site("Storage Site", NodeType.StorageSite, 5),
           new Site("Carpenter Site", NodeType.CarpenterSite, 5),
           new Site("Blacksmith Site", NodeType.BlacksmithSite, 5),
        };
        Edge[] edgeMap;
        float constructionProgression;
        float constructionTime;
        List<Agent> agentList = new List<Agent>();

        Edge currentEdgeToEdit;
        Agent selectedAgent = null; //used to highlight the current selected agent for the User to remove

        #region Setup Methods
        public Form1() => InitializeComponent();

        private void Form1_Load(object sender, EventArgs e)
        {
            inst = this;
            CreateEdges();

            for (int i = 0; i < siteMap.Length; i++)
                panel_SitesList.Controls.Add(SitePanel(siteMap[i], i));
        }

        //assigning the edge's cost and connected nodes
        void CreateEdges()
        {
            edgeMap = new Edge[]
            {
               new Edge(EdgeName.GateOne,5, nodeMap[nodeIndex(NodeType.SouthGateNode)], nodeMap[nodeIndex(NodeType.ForestSouthNode)]),
               new Edge(EdgeName.GateTwo,5, nodeMap[nodeIndex(NodeType.ForestCenterNode)], nodeMap[nodeIndex(NodeType.SouthGateNode)]),
               new Edge(EdgeName.GateThree,7, siteMap[nodeIndex(NodeType.ForestSite)], nodeMap[nodeIndex(NodeType.NorthGateNode)]),

               new Edge(EdgeName.ResidentStreetOne,6, siteMap[nodeIndex(NodeType.BlacksmithSite)], nodeMap[nodeIndex(NodeType.SouthGateNode)]),
               new Edge(EdgeName.ResidentStreetTwo,11, nodeMap[nodeIndex(NodeType.ResidentStreetNode)], siteMap[nodeIndex(NodeType.BlacksmithSite)]),
               new Edge(EdgeName.ResidentStreetThree,9, siteMap[nodeIndex(NodeType.MainSite)], nodeMap[nodeIndex(NodeType.ResidentStreetNode)]),

               new Edge(EdgeName.InterStreetOne,6, nodeMap[nodeIndex(NodeType.InterStreetNode)], siteMap[nodeIndex(NodeType.BlacksmithSite)]),
               new Edge(EdgeName.InterStreetTwo,6, nodeMap[nodeIndex(NodeType.InterStreetNode)], siteMap[nodeIndex(NodeType.StorageSite)]),
               new Edge(EdgeName.InterStreetThree,2, siteMap[nodeIndex(NodeType.StorageSite)], siteMap[nodeIndex(NodeType.MainSite)]),
               new Edge(EdgeName.InterStreetFour,2, siteMap[nodeIndex(NodeType.StorageSite)], nodeMap[nodeIndex(NodeType.WestCornerNode)]),

               new Edge(EdgeName.ForestOne,5, nodeMap[nodeIndex(NodeType.ForestSouthNode)], siteMap[nodeIndex(NodeType.MiningSite)]),
               new Edge(EdgeName.ForestTwo,6, siteMap[nodeIndex(NodeType.MiningSite)], nodeMap[nodeIndex(NodeType.ForestCenterNode)]),
               new Edge(EdgeName.ForestThree,2, nodeMap[nodeIndex(NodeType.ForestCenterNode)], nodeMap[nodeIndex(NodeType.ForestNorthNode)]),
               new Edge(EdgeName.ForestFour,2, nodeMap[nodeIndex(NodeType.ForestNorthNode)], siteMap[nodeIndex(NodeType.ForestSite)]),

               new Edge(EdgeName.WoodStreetOne,5, siteMap[nodeIndex(NodeType.CarpenterSite)], nodeMap[nodeIndex(NodeType.NorthGateNode)]),
               new Edge(EdgeName.WoodStreetTwo,3, nodeMap[nodeIndex(NodeType.SouthGateNode)], siteMap[nodeIndex(NodeType.CarpenterSite)]),

               new Edge(EdgeName.NorthStreetOne,6, nodeMap[nodeIndex(NodeType.NorthGateNode)], nodeMap[nodeIndex(NodeType.WestCornerNode)]),
               new Edge(EdgeName.NorthStreetTwo,3, nodeMap[nodeIndex(NodeType.WestCornerNode)], siteMap[nodeIndex(NodeType.MainSite)]),

               new Edge(EdgeName.LuxStreetOne,4, nodeMap[nodeIndex(NodeType.NorthGateNode)], nodeMap[nodeIndex(NodeType.LuxStreetNode)]),
               new Edge(EdgeName.LuxStreetTwo,5, nodeMap[nodeIndex(NodeType.LuxStreetNode)], nodeMap[nodeIndex(NodeType.InterStreetNode)]),
            };
        }

        //returns the node or site based on it's nodeType
        int nodeIndex(NodeType type)
        {
            int i = nodeMap.ToList().FindIndex(n => n.nodeType == type);
            if (i > -1) return i; //-1 represents that nodeMap does not contain that nodeType
            return siteMap.ToList().FindIndex(s => s.nodeType == type);
        }
        #endregion

        //returns the sites which the agent(transporter) can take out materials from
        public Site[] TakeOutSites => siteMap.ToList().GetRange(1, siteMap.Length - 1).ToArray();

        //Called upon Form2 closing, the agent at the index in agentList will be updated
        public void UpdateAgent(Agent agent, int index)
        {
            agentList[index] = agent;
            agentList[index].listBox.mainJob.Image = IconPath.GetIcon(agent.MainJob.jobName);
            agentList[index].listBox.agentLabel.Text = agent.name;
        }

        //Called upon Form2 opening, get the agent's index in the agentList
        public int GetAgentIndex(Agent agent) => agentList.IndexOf(agent);
        //assign the selected to the currently selected agent by the user
        public void AgentSelected(Agent agent) => selectedAgent = agent;
        //
        public bool AgentAlreadySelected(Agent agent)
        {
            if (selectedAgent == agent)
                return true;
            //if the selected agent isn't the current selected agent
            if (selectedAgent != null)  //change the current selected agent's box colour to its default colour
                selectedAgent.listBox.agentBox.BackColor = SystemColors.Control;
            return false;
        }

        //Event called from pressing the button to create a new agent
        private void button_AddAgent_Click(object sender, EventArgs e)
        {
            Agent newAgent = new Agent($"Agent {agentList.Count}");
            agentList.Add(newAgent);
            panel_AgentList.Controls.Add(AgentPanel(newAgent));
        }

        private void numeric_Edge_ValueChanged(object sender, EventArgs e) => currentEdgeToEdit.cost = (int)numeric_Edge.Value;

        #region Create Panels (Agent, Site, Edge)
        //Creating the groupbox that will hold a summary of the agent's data
        GroupBox AgentPanel(Agent agent)
        {
            GroupBox agentBox = new GroupBox();
            int yPoint = agentList.Count > 1 ? ((agentList.Count - 1) * 70) + 3 : 3;
            agentBox.Location = new Point(3, yPoint);
            agentBox.Size = new Size(254, 70);
            agentBox.DoubleClick += agent.DisplayAgentInformation;
            agentBox.Click += agent.AgentSelect;

            PictureBox mainJob = new PictureBox();
            agentBox.Controls.Add(mainJob);
            mainJob.Location = new Point(6, 13);
            mainJob.Size = new Size(50, 50);
            mainJob.SizeMode = PictureBoxSizeMode.Zoom;
            mainJob.Image = IconPath.GetIcon(agent.MainJob.jobName);

            Label agentLabel = new Label();
            agentBox.Controls.Add(agentLabel);
            agentLabel.Location = new Point(62, 16);
            agentLabel.Text = agent.name;

            GroupBox progressBox = new GroupBox();
            agentBox.Controls.Add(progressBox);
            progressBox.Location = new Point(62, 31);
            progressBox.Size = new Size(188, 32);
            progressBox.Hide();

            ProgressBar progressBar = new ProgressBar();
            progressBox.Controls.Add(progressBar);
            progressBar.Location = new Point(3, 10);
            progressBar.Size = new Size(158, 17);

            PictureBox progressJob = new PictureBox();
            progressBox.Controls.Add(progressJob);
            progressJob.Location = new Point(165, 8);
            progressJob.Size = new Size(20, 20);
            progressJob.SizeMode = PictureBoxSizeMode.Zoom;

            agent.listBox.agentBox = agentBox;
            agent.listBox.mainJob = mainJob;
            agent.listBox.agentLabel = agentLabel;
            agent.listBox.progressBox = progressBox;
            agent.listBox.progressBar = progressBar;
            agent.listBox.progressJob = progressJob;

            return agentBox;
        }

        //Creates a group box that will display the site's inventory and number of agents in the site
        GroupBox SitePanel(Site site, int i)
        {
            GroupBox siteBox = new GroupBox();
            int yPoint = i > 0 ? (i * 75) + 3 : 3;
            siteBox.Location = new Point(3, yPoint);
            siteBox.Size = new Size(235, 75);
            siteBox.Click += new EventHandler(site.DisplaySiteInformation);

            PictureBox siteIcon = new PictureBox();
            siteBox.Controls.Add(siteIcon);
            siteIcon.Location = new Point(6, 11);
            siteIcon.Size = new Size(30, 30);
            siteIcon.SizeMode = PictureBoxSizeMode.Zoom;
            siteIcon.Image = IconPath.GetIcon(site.nodeType);

            Label siteName = new Label();
            siteBox.Controls.Add(siteName);
            siteName.Location = new Point(42, 11);
            siteName.Text = site.name;

            Label workers = new Label();
            siteBox.Controls.Add(workers);
            workers.Location = new Point(42, 28);
            workers.BringToFront();
            workers.Size = new Size(70, 13);
            workers.Text = $"{site.currentAgents.Count}/{site.maxAgents}";

            int xPoint = 6; //adaptively set the positions of the groupBoxs if a site does not use a material
            if (SiteHoldsOre(site.nodeType))
            {
                siteBox.Controls.Add(CreateMaterialBox(site, MaterialType.Ore, xPoint));
                xPoint += 57;
            }
            if (SiteHoldsIngot(site.nodeType))
            {
                siteBox.Controls.Add(CreateMaterialBox(site, MaterialType.Ingot, xPoint));
                xPoint += 57;
            }
            if (SiteHoldsWood(site.nodeType))
            {
                siteBox.Controls.Add(CreateMaterialBox(site, MaterialType.Wood, xPoint));
                xPoint += 57;
            }
            if (SiteHoldsPlank(site.nodeType))
                siteBox.Controls.Add(CreateMaterialBox(site, MaterialType.Plank, xPoint));

            return siteBox;
        }

        //creating the group boxes to display the materials for the sites
        GroupBox CreateMaterialBox(Site site, MaterialType matType, int xPoint)
        {
            GroupBox materialBox = new GroupBox();
            materialBox.Location = new Point(xPoint, 42);
            materialBox.Size = new Size(51, 27);

            PictureBox icon = new PictureBox();
            materialBox.Controls.Add(icon);
            icon.Location = new Point(1, 7);
            icon.Size = new Size(20, 20);
            icon.SizeMode = PictureBoxSizeMode.Zoom;
            icon.Image = IconPath.GetIcon(matType);

            Label count = new Label();
            materialBox.Controls.Add(count);
            count.Location = new Point(23, 10);
            count.Text = "00";
            count.Font = new Font(count.Font.FontFamily, 7);

            //Assigning material box so that it can be updated at runtime
            switch (matType)
            {
                case MaterialType.Wood:
                    site.inventory.wood = new MaterialBox(materialBox, icon, count, MaterialType.Wood);
                    break;
                case MaterialType.Plank:
                    site.inventory.plank = new MaterialBox(materialBox, icon, count, MaterialType.Plank);
                    break;
                case MaterialType.Ore:
                    site.inventory.ore = new MaterialBox(materialBox, icon, count, MaterialType.Ore);
                    break;
                case MaterialType.Ingot:
                    site.inventory.ingot = new MaterialBox(materialBox, icon, count, MaterialType.Ingot);
                    break;
            }
            return materialBox;
        }
        public bool SiteHoldsWood(NodeType type) => type == NodeType.ForestSite || type == NodeType.CarpenterSite || type == NodeType.StorageSite;
        public bool SiteHoldsPlank(NodeType type) => type == NodeType.CarpenterSite || type == NodeType.StorageSite || type == NodeType.MainSite;
        public bool SiteHoldsOre(NodeType type) => type == NodeType.MiningSite || type == NodeType.BlacksmithSite || type == NodeType.StorageSite;
        public bool SiteHoldsIngot(NodeType type) => type == NodeType.BlacksmithSite || type == NodeType.StorageSite || type == NodeType.MainSite;
        #endregion

        #region Edge Events
        //Display the edge's information and set it's position
        private void OpenEdgeBox(Edge edge, Label label)
        {
            currentEdgeToEdit = edge;
            groupBox_EdgeBox.Location = new Point(label.Location.X + label.Size.Width, label.Location.Y);
            groupBox_EdgeBox.Show();
            numeric_Edge.Value = edge.cost;
            pictureBox_EdgePointOne.Image = IconPath.GetIcon(edge.pointOne.nodeType);
            pictureBox_EdgePointTwo.Image = IconPath.GetIcon(edge.pointTwo.nodeType);
        }

        //Display the edge's inforamtion and set it's position
        //point is used to ensure that the box is within view
        private void OpenEdgeBox(Edge edge, Label label, Point point)
        {
            currentEdgeToEdit = edge;
            groupBox_EdgeBox.Location = new Point(label.Location.X + point.X, label.Location.Y + point.Y);
            groupBox_EdgeBox.Show();
            numeric_Edge.Value = edge.cost;
            pictureBox_EdgePointOne.Image = IconPath.GetIcon(edge.pointOne.nodeType);
            pictureBox_EdgePointTwo.Image = IconPath.GetIcon(edge.pointTwo.nodeType);
        }

        private void panel_Map_Click(object sender, EventArgs e) => groupBox_EdgeBox.Hide();

        #region Edge Label Clicks Events
        //Label events which will call OpenEdgeBox and pass in the edge and the label which will be used to determine the position
        private void label_EdgeForestOne_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.ForestOne), label_EdgeForestOne);
        private void label_EdgeForestTwo_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.ForestTwo), label_EdgeForestTwo);
        private void label_EdgeForestThree_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.ForestThree), label_EdgeForestThree);
        private void label_EdgeForestFour_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.ForestFour), label_EdgeForestFour);
        private void label_EdgeGateOne_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.GateOne), label_EdgeGateOne);
        private void label_EdgeGateTwo_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.GateTwo), label_EdgeGateTwo);
        private void label_EdgeGateThree_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.GateThree), label_EdgeGateThree);
        private void label_EdgeWoodStreetOne_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.WoodStreetOne), label_EdgeWoodStreetOne);
        private void label_EdgeWoodStreetTwo_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.WoodStreetTwo), label_EdgeWoodStreetTwo);
        private void label_EdgeInterStreetOne_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.InterStreetOne), label_EdgeInterStreetOne);
        private void label_EdgeInterStreetTwo_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.InterStreetTwo), label_EdgeInterStreetTwo);
        private void label_EdgeInterStreetThree_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.InterStreetThree), label_EdgeInterStreetThree, new Point(-20, label_EdgeInterStreetThree.Size.Height));
        private void label_EdgeInterStreetFour_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.InterStreetFour), label_EdgeInterStreetFour);
        private void label_EdgeNorthStreetOne_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.NorthStreetOne), label_EdgeNorthStreetOne);
        private void label_EdgeNorthStreetTwo_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.NorthStreetTwo), label_EdgeNorthStreetTwo);
        private void label_EdgeLuxStreetOne_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.LuxStreetOne), label_EdgeLuxStreetOne);
        private void label_EdgeLuxStreetTwo_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.LuxStreetTwo), label_EdgeLuxStreetTwo);
        private void label_EdgeResidentStreetOne_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.ResidentStreetOne), label_EdgeResidentStreetOne);
        private void label_EdgeResidentStreetTwo_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.ResidentStreetTwo), label_EdgeResidentStreetTwo, new Point(0, label_EdgeResidentStreetTwo.Size.Height + 5));
        private void label_EdgeResidentStreetThree_Click(object sender, EventArgs e) => OpenEdgeBox(edgeMap.Single(s => s.name == EdgeName.ResidentStreetThree), label_EdgeResidentStreetThree, new Point(-40, label_EdgeResidentStreetThree.Size.Height));
        #endregion

        //checks if there is Form3 opened with the site's name
        public bool SiteFormAlreadyOpened(string name)
        {
            foreach (Form form in Application.OpenForms)
                if (form.Name.Equals($"Form3_{name}"))
                    return true;
            return false;
        }

        //checks if there is a Form2 opened with the agent's HashCode
        public bool AgentFormAlreadyOpened(string name)
        {
            foreach (Form form in Application.OpenForms)
                if (form.Name.Equals($"Form2_{name}"))
                    return true;
            return false;
        }
        #endregion

        #region Removing Agent Event
        //Remove an agent from the agentList and it's groupBox and reorganize the list
        private void button_RemoveAgent_Click(object sender, EventArgs e)
        {
            if (selectedAgent == null)
                return;
            groupBox_Agents.Controls.Remove(selectedAgent.listBox.agentBox);
            selectedAgent.listBox.agentBox.Dispose();
            agentList.Remove(selectedAgent);
            //organize the agent's list
            for (int i = 0; i < agentList.Count; i++)
                agentList[i].listBox.agentBox.Location = new Point(3, i > 0 ? (i * 75) + 3 : 3);
        }
        #endregion

        //returns the center position of the chosen node
        //https://stackoverflow.com/a/3118035
        Point ReturnNodeLocation(PictureBox node)
        {
            if (node.InvokeRequired) return (Point)node.Invoke((Func<Point>)delegate { return new Point(node.Location.X + node.Size.Width / 2, node.Location.Y + node.Size.Height / 2); });
            else return new Point(node.Location.X + node.Size.Width / 2, node.Location.Y + node.Size.Height / 2);
        }

        //gets the position on the form of where the node's PictureBox is
        public Point GetNodeLocation(NodeType node)
        {
            if (node == NodeType.MainSite)
                return ReturnNodeLocation(pictureBox_SiteMain);
            if (node == NodeType.BlacksmithSite)
                return ReturnNodeLocation(pictureBox_SiteBlacksmith);
            if (node == NodeType.CarpenterSite)
                return ReturnNodeLocation(pictureBox_SiteCarpenter);
            if (node == NodeType.StorageSite)
                return ReturnNodeLocation(pictureBox_SiteStorage);
            if (node == NodeType.ForestSite)
                return ReturnNodeLocation(pictureBox_SiteForest);
            if (node == NodeType.MiningSite)
                return ReturnNodeLocation(pictureBox_SiteMine);

            if (node == NodeType.ForestCenterNode)
                return ReturnNodeLocation(pictureBox_NodeForestCentral);
            if (node == NodeType.ForestNorthNode)
                return ReturnNodeLocation(pictureBox_NodeForestNorth);
            if (node == NodeType.ForestSouthNode)
                return ReturnNodeLocation(pictureBox_NodeForestSouth);
            if (node == NodeType.InterStreetNode)
                return ReturnNodeLocation(pictureBox_NodeInterStreet);
            if (node == NodeType.LuxStreetNode)
                return ReturnNodeLocation(pictureBox_NodeLuxStreet);
            if (node == NodeType.NorthGateNode)
                return ReturnNodeLocation(pictureBox_NodeNorthGate);
            if (node == NodeType.ResidentStreetNode)
                return ReturnNodeLocation(pictureBox_NodeResidentStreet);
            if (node == NodeType.SouthGateNode)
                return ReturnNodeLocation(pictureBox_NodeSouthGate);
            if (node == NodeType.WestCornerNode)
                return ReturnNodeLocation(pictureBox_NodeWestCorner);
            return new Point();
        }

        //gets the job's site by JobName
        public Site GetSite(JobName job)
        {
            switch (job)
            {
                case JobName.Carpenter:
                    return siteMap.Single(s => s.nodeType == NodeType.CarpenterSite);
                case JobName.Logger:
                    return siteMap.Single(s => s.nodeType == NodeType.ForestSite);
                case JobName.Blacksmith:
                    return siteMap.Single(s => s.nodeType == NodeType.BlacksmithSite);
                case JobName.Miner:
                    return siteMap.Single(s => s.nodeType == NodeType.MiningSite);
                case JobName.Constructor:
                    return siteMap.Single(s => s.nodeType == NodeType.MainSite);
                case JobName.Transporter:
                    return siteMap.Single(s => s.nodeType == NodeType.StorageSite);
                default:
                    return null;
            }
        }
        //gets the site by its nodeType
        public Site GetSite(NodeType job) => siteMap.Single(s => s.nodeType == job);

        //returns the edges that are connected to the node
        public Edge[] GetConnectedEdges(Node node)
        {
            List<Edge> connectedEdges = new List<Edge>();
            foreach (var edge in edgeMap)
                if (edge.HasNode(node))
                    connectedEdges.Add(edge);
            return connectedEdges.ToArray();
        }

        //simply checks if the current node is a site
        public bool CurrentNodeIsSite(NodeType type)
        {
            switch (type)
            {
                case NodeType.MainSite:
                case NodeType.BlacksmithSite:
                case NodeType.CarpenterSite:
                case NodeType.StorageSite:
                case NodeType.ForestSite:
                case NodeType.MiningSite:
                    return true;
                default:
                    return false;
            }
        }

        //returns the edge's cost if it holds both pointOne and pointTwo
        public int GetEdgeCost(Node pointOne, Node pointTwo)
        {
            foreach (var edge in edgeMap)
                if (edge.pointOne == pointOne && edge.pointTwo == pointTwo)
                    return edge.cost;
            return -1;
        }

        //creates a pictureBox which will be used to display the agent when traveling
        public PictureBox CreateAgentIcon()
        {
            PictureBox agentIcon = new PictureBox();
            agentIcon.SizeMode = PictureBoxSizeMode.Zoom;
            agentIcon.BackColor = Color.Transparent;
            agentIcon.Image = IconPath.agentIcon;
            agentIcon.Size = new Size(15, 15);
            agentIcon.BringToFront();
            agentIcon.Hide();

            if (panel_Map.InvokeRequired) panel_Map.Invoke((MethodInvoker)delegate { panel_Map.Controls.Add(agentIcon); });
            else panel_Map.Controls.Add(agentIcon);

            return agentIcon;
        }

        //public void SetLabelAngle(string text)
        //{
        //    if (label_Angle.InvokeRequired) label_Angle.Invoke(new Action<string>(SetLabelAngle), text);
        //    else label_Angle.Text = text;
        //}

        //Starts the programs
        private void button_Start_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < agentList.Count; i++)
            {
                ThreadStart startThread = new ThreadStart(agentList[i].InitAgent);
                Thread newThread = new Thread(startThread);
                newThread.Start();
            }
        }
    }
}
