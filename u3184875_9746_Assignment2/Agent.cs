using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    public class Agent
    {
        public string name = null;
        public bool isTraveling = false;
        const float moveSpeed = 10f; // meters per second
        //slowest speed must be 4, any value lower then that then the calculations will be broken.
        //this is because Point can only take in Integers and not decimal values

        protected Job currentJob;
        public JobName GetCurrentJobName => currentJob.jobName;
        protected Job mainJob;
        public Job MainJob { get => mainJob; set => mainJob = value; }
        Job[] subJobs;
        public Job[] SubJobs { get => subJobs; set => subJobs = value; }

        //this list holds jobs that the agent could not do and prevent the agent from going back to the same job after going to another job
        List<JobName> blackListJobs = new List<JobName>();

        Path currentPath;
        List<Path> sitePaths = new List<Path>();
        protected CurrentNode currentNode;
        protected Destination<Site> targetSite;
        Destination<Node> currentDestination;

        protected Inventory inventory;
        public Inventory Inventory { get => inventory; set => inventory = value; }

        public PictureBox agentIcon; //used to display the location of the agent when they are moving on the map
        public AgentListBox listBox;
        public ProgressBar siteProgressBar;

        //used to determine which node to take that is within the angle
        const double viewAngle = 50;

        protected bool deliveringMaterial = false;

        public Agent(string name)
        {
            this.name = name;
            inventory = new Inventory();
            mainJob = new Job(JobName.Blacksmith, new BlackSmith(inventory, Form1.inst.GetSiteByJob(JobName.Blacksmith)), 5);
        }

        public Agent(Agent agent)
        {
            name = agent.name;
            mainJob = agent.mainJob;
            subJobs = agent.subJobs;
            inventory = agent.inventory;

            agentIcon = agent.agentIcon;
            listBox = agent.listBox;
            siteProgressBar = agent.siteProgressBar;
        }

        public virtual void InitAgent()
        {
            agentIcon = Form1.inst.CreateAgentIcon();

            blackListJobs = new List<JobName>();
            currentNode = new CurrentNode(mainJob.jobClass.jobSite, Form1.inst.GetNodeLocation(mainJob.SiteNodeType));
            currentJob = mainJob;
            FindJob();
        }

        //Finding a job the agent can do based on their skill level if agent can't do its main job
        protected virtual void FindJob()
        {
            //if agent can't do main job search through the sub jobs
            if (blackListJobs.Contains(mainJob.jobName) && subJobs != null)
            {
                Job[] sortedSub = subJobs.OrderBy(o => o.skillLevel).ToArray();
                foreach (var job in sortedSub)
                    if (!blackListJobs.Contains(job.jobName))
                    {
                        Site jobSite = Form1.inst.GetSiteByJob(job.jobName);
                        if (jobSite.nodeType != NodeType.StorageSite)
                        {
                            targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                            currentJob = job;
                            break;
                        }
                    }
            }
            else
            {
                Site jobSite = Form1.inst.GetSiteByJob(mainJob.jobName);
                if (jobSite.nodeType != NodeType.StorageSite)
                {
                    targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                    currentJob = mainJob;
                }
            }

            PathFinding();
        }

        //Checks the conditions to do the job before doing it
        protected virtual void StartJob()
        {
            if (deliveringMaterial)
            {
                Task.Run(currentJob.jobClass.DeliverMaterial).Wait();
                deliveringMaterial = false;
                Form1.inst.SetLabelAngle("Finish Delivering");
                FindJob();
            }
            else
            {
                if (currentJob.jobClass.jobSite.HasSpace())
                {
                    if (!currentJob.jobClass.HasSpaceForMaterial())
                    {
                        Form1.inst.SetLabelAngle("Take out Materials");
                        Task.Run(currentJob.jobClass.TakeOutMaterial).Wait();
                        targetSite = new Destination<Site>(Form1.inst.GetSiteByNodeType(NodeType.StorageSite), Form1.inst.GetNodeLocation(NodeType.StorageSite));
                        deliveringMaterial = true;
                        PathFinding();
                        return;
                    }
                    else
                    {
                        if (currentJob.jobClass.HasEnoughMaterial())
                        {
                            Task.Run(currentJob.jobClass.ProgressJob).Wait();
                            Form1.inst.SetLabelAngle("Job Complete");
                            blackListJobs.Clear();
                            FindJob();
                            return;
                        }
                    }
                }

                blackListJobs.Add(currentJob.jobName);
                if (blackListJobs.Count == subJobs.Length + 1) //+1 is the mainJob
                    blackListJobs.Clear();
                Form1.inst.SetLabelAngle("Find new Job");
                FindJob();
            }
        }

        #region Path Finding
        //Checks whether the agent is already at the targetSite, has already created a found to the targetSite or will create a new path to the targetSite
        protected void PathFinding()
        {
            //If agent is already at the target site
            if (currentNode.node == targetSite.nodeTarget)
                StartJob();
            else
            {
                currentPath = new Path();
                foreach (var path in sitePaths)
                    if (path.start == currentNode.node && path.end == targetSite.nodeTarget)
                        currentPath = path;

                if (!currentPath.Equals(new Path()))
                {
                    Form1.inst.SetLabelAngle($"Use Existing Path: {currentPath.start.nodeType} - {currentPath.end.nodeType}");
                    TravelExistingPath();
                }
                else
                {
                    Path newPath = new Path(currentNode.node, targetSite.nodeTarget, new List<Node>());
                    newPath.nodes.Add(currentNode.node);
                    currentPath = newPath;
                    Form1.inst.SetLabelAngle($"Travel New Path: {currentPath.start.nodeType} - {targetSite.nodeTarget.nodeType}");
                    FindNewPath();
                }
            }
        }

        void TravelExistingPath()
        {
            List<Node> path = currentPath.nodes;
            for (int i = 0; i < path.Count; i++)
            {
                Point destPoint = Form1.inst.GetNodeLocation(path[i].nodeType);
                currentDestination = new Destination<Node>(path[i], destPoint);
                Task.Run(MoveToDestination).Wait();

                currentNode = new CurrentNode(path[i], destPoint);
            }
            Form1.inst.SetLabelAngle("Existing Path Complete");
            StartJob();
        }

        void FindNewPath()
        {
            Node closestNode = GetClosestNodeFromAngle();
            Point destPoint = Form1.inst.GetNodeLocation(closestNode.nodeType);
            currentDestination = new Destination<Node>(closestNode, destPoint);

            Task.Run(MoveToDestination).Wait();

            currentNode = new CurrentNode(closestNode, destPoint);
            currentPath.nodes.Add(closestNode);
            if (currentNode.node.nodeType == targetSite.nodeTarget.nodeType)
            {
                Form1.inst.SetLabelAngle("New Path Complete");
                sitePaths.Add(currentPath);
                StartJob();
                return;
            }
            //checking if currentNode is a site and creating a path to the site from start
            if (Form1.inst.CurrentNodeIsSite(currentNode.node.nodeType))
            {
                Path existingPath = new Path();
                foreach (var path in sitePaths)
                    if (path.start == currentPath.start && path.end == closestNode)
                        existingPath = path;

                if (!existingPath.Equals(new Path()))
                {
                    if (PathCost(currentPath) < PathCost(existingPath))
                    {
                        Path nodePath = currentPath;
                        nodePath.end = closestNode;
                        existingPath = nodePath;
                    }
                }
                else
                {
                    Path nodePath = currentPath;
                    nodePath.end = closestNode;
                    sitePaths.Add(nodePath);  //change dictinary to llist
                }
            }

            FindNewPath();
        }

        int PathCost(Path path)
        {
            int totalCost = 0;
            for (int i = 0; i < path.nodes.Count - 1; i++)
                totalCost += Form1.inst.GetEdgeCost(path.nodes[i], path.nodes[i + 1]);
            return totalCost;
        }

        //Returns the closest node from currentNode to targetSite
        Node GetClosestNodeFromAngle()
        {
            double angleToTargetSite = AngleToNode(targetSite.targetPosition);
            double leftAngle = angleToTargetSite - (viewAngle / 2);
            double rightAngle = angleToTargetSite + (viewAngle / 2);

            List<Edge> inViewEdges = new List<Edge>();
            Edge[] connectedEdges = Form1.inst.GetConnectedEdges(currentNode.node);
            foreach (Edge edge in connectedEdges)
            {
                //checking of the connected edge has already been visited before checking of the edge is within view
                Node otherPoint = edge.GetOtherPoint(currentNode.node);
                if (!currentPath.nodes.Contains(otherPoint))
                {
                    if (otherPoint.nodeType == targetSite.nodeTarget.nodeType)
                        return otherPoint;
                    double angleToPoint = AngleToNode(Form1.inst.GetNodeLocation(otherPoint.nodeType));
                    if (angleToPoint > leftAngle)
                    {
                        //in the situation where rightAngle is over 360, we would need to add 360 to angleToNeighbour as it's angle would be in the range of 0 - 90
                        //for example: rightAngle = 390 and angleToNeighbour = 20, adding 360 to angleToNeighbour == 380 making it in range of viewAngle
                        //it is also to prevent the checker from always returning true as angleToNeighbour will always be smaller then rightAngle
                        if (rightAngle > 360)
                            angleToPoint += 360;
                        if (angleToPoint < rightAngle)
                            inViewEdges.Add(edge);
                    }
                }
            }

            Edge shortestEdge = null;
            if (inViewEdges.Count > 0)
            {
                shortestEdge = inViewEdges.First();
                foreach (Edge edge in inViewEdges)
                    if (edge.cost < shortestEdge.cost)
                        shortestEdge = edge;
            }
            else
            {
                shortestEdge = connectedEdges.First();
                double closestAngle = int.MaxValue;
                foreach (Edge edge in connectedEdges)
                {
                    Node otherPoint = edge.GetOtherPoint(currentNode.node);
                    if (!currentPath.nodes.Contains(otherPoint))
                    {
                        double angleToPoint = AngleToNode(Form1.inst.GetNodeLocation(otherPoint.nodeType));
                        if (Math.Abs(angleToTargetSite - angleToPoint) < Math.Abs(angleToTargetSite - closestAngle))
                        {
                            closestAngle = angleToPoint;
                            shortestEdge = edge;
                        }
                    }
                }
            }

            return shortestEdge.GetOtherPoint(currentNode.node);
        }

        //Reference https://answers.unity.com/questions/414829/any-one-know-maths-behind-this-movetowards-functio.html
        async Task MoveToDestination()
        {
            ShowAgentIcon(true);
            AgentIcon_Location = currentNode.position;
            double length = int.MaxValue;
            Point agentIconLocation = AgentIcon_Location;
            while (length > 0)
            {
                double x = currentDestination.targetPosition.X - (agentIconLocation.X);
                double y = currentDestination.targetPosition.Y - (agentIconLocation.Y);
                length = Math.Sqrt(x * x + y * y);

                if (length <= moveSpeed)
                    break;

                agentIconLocation.X = agentIconLocation.X + (int)Math.Round(x / length * moveSpeed);
                agentIconLocation.Y = agentIconLocation.Y + (int)Math.Round(y / length * moveSpeed);

                AgentIcon_Location = new Point(agentIconLocation.X - 7, agentIconLocation.Y - 7);
                await Task.Delay(100);
            }
            ShowAgentIcon(false);
        }
        #endregion

        //https://stackoverflow.com/questions/7609839/accessing-a-forms-control-from-a-separate-thread
        void ShowAgentIcon(bool show)
        {
            if (agentIcon.InvokeRequired)
                agentIcon.Invoke(new Action<bool>(ShowAgentIcon), show);
            else
            {
                if (show) agentIcon.Show();
                else agentIcon.Hide();
            }
        }

        //https://stackoverflow.com/questions/765225/how-to-get-the-handle-of-the-form-with-getset
        delegate Point GetAgentIconLocation();
        Point AgentIcon_Location
        {
            get
            {
                if (agentIcon.InvokeRequired)
                    return (Point)agentIcon.Invoke((GetAgentIconLocation)delegate
                    { return new Point(agentIcon.Location.X, agentIcon.Location.Y); });
                else
                    return new Point(agentIcon.Location.X, agentIcon.Location.Y);
            }
            set
            {
                if (agentIcon.InvokeRequired)
                    agentIcon.Invoke(new Action(() => { agentIcon.Location = value; }));
                else
                    agentIcon.Location = value;
            }
        }

        //Reference https://stackoverflow.com/a/42070816
        double AngleToNode(Point target)
        {
            double x = target.X - currentNode.position.X;
            double y = target.Y - currentNode.position.Y;

            double rads = Math.Atan2(y, x);
            if (y < 0)
                rads = Math.PI * 2 + rads;
            return rads * 180 / Math.PI;
        }

        public void DisplayAgentInformation(object sender, EventArgs e)
        {
            if (!Form1.inst.AgentFormAlreadyOpened(GetHashCode().ToString()))
            {
                Form2 form2 = new Form2(this);
                form2.Name = $"Form2_{GetHashCode()}";
                form2.Show();
            }
        }

        public void AgentSelect(object sender, EventArgs e)
        {
            if (!Form1.inst.AgentAlreadySelected(this))
            {
                Form1.inst.AgentSelected(this);
                listBox.agentBox.BackColor = Color.FromArgb(49, 113, 170);
            }
            else
            {
                Form1.inst.AgentSelected(null);
                listBox.agentBox.BackColor = SystemColors.Control;
            }
        }
    }

    public class Transporter : Agent
    {
        MaterialType materialToDeliver;

        //similar to the visitedJobList, this list will hold the sites that the agent could not take materials out from
        List<NodeType> blacklistSites = new List<NodeType>();

        public Transporter(Agent agent) : base(agent) { }

        public override void InitAgent()
        {
            agentIcon = Form1.inst.CreateAgentIcon();

            blacklistSites = new List<NodeType>();
            currentNode = new CurrentNode(mainJob.jobClass.jobSite, Form1.inst.GetNodeLocation(mainJob.SiteNodeType));
            FindJob();
        }

        //assigning the target site for the agent to go to
        protected override void FindJob()
        {
            if (deliveringMaterial)
            {
                NodeType currentNodeType = currentNode.node.nodeType;
                if (currentNodeType == NodeType.StorageSite)
                {
                    if (materialToDeliver == MaterialType.Ingot || materialToDeliver == MaterialType.Plank)
                        SetTargetSite(NodeType.MainSite);
                    else if (materialToDeliver == MaterialType.Wood)
                        SetTargetSite(NodeType.CarpenterSite);
                    else if (materialToDeliver == MaterialType.Ore)
                        SetTargetSite(NodeType.BlacksmithSite);
                }
                else if (currentNodeType == NodeType.BlacksmithSite && materialToDeliver == MaterialType.Ingot)
                    SetTargetSite(NodeType.StorageSite);
                else if (currentNodeType == NodeType.CarpenterSite && materialToDeliver == MaterialType.Plank)
                    SetTargetSite(NodeType.StorageSite);
                else if (currentNodeType == NodeType.ForestSite && materialToDeliver == MaterialType.Wood)
                    SetTargetSite(NodeType.CarpenterSite);
                else if (currentNodeType == NodeType.MiningSite && materialToDeliver == MaterialType.Ore)
                    SetTargetSite(NodeType.BlacksmithSite);
            }
            else if (blacklistSites.Contains(currentNode.node.nodeType)) //if agent isnt at the current site to deliver materials
                SetTargetSite(NodeType.StorageSite); //if the current site has been blacklisted go back to the storage site
            PathFinding();
        }

        void SetTargetSite(NodeType type)
        {
            mainJob.jobClass.jobSite = Form1.inst.GetSiteByNodeType(type);
            targetSite = new Destination<Site>(Form1.inst.GetSiteByNodeType(type), Form1.inst.GetNodeLocation(type));
        }

        //Checks if the agent is at the site to deliver or take out materials
        protected override void StartJob()
        {
            if (!deliveringMaterial)
            {
                //if site is blacksmith, carpenter, forest or mining
                if (mainJob.SiteNodeType != NodeType.StorageSite && mainJob.jobClass.HasEnoughMaterial())
                    SetMaterialToDeliver();
                else if (mainJob.SiteNodeType == NodeType.StorageSite)
                {   //find which material has the highest amount to take out
                    Site storageSite = mainJob.jobClass.jobSite;
                    MaterialBox material = new MaterialBox();
                    MaterialBox[] inventoryArray = { storageSite.inventory.ingot, storageSite.inventory.ore, storageSite.inventory.wood, storageSite.inventory.plank };
                    foreach (var mat in inventoryArray)
                        if (mat.HasAmount(mainJob.jobClass.TakeOutAmount))
                            if (material.Current < mat.Current)
                                material = mat;
                    //if a material box was not selected
                    if (material.Equals(new MaterialBox()))
                    {
                        blacklistSites.Add(currentNode.node.nodeType);
                        FindJob();
                        return;
                    }
                    materialToDeliver = material.materialType;
                }
                else //if site does not have enough materials to take out and site is not storage site
                {
                    blacklistSites.Add(currentNode.node.nodeType);
                    FindJob();
                    return;
                }

                mainJob.jobClass.TakeOutMaterial(materialToDeliver).Wait();
                deliveringMaterial = true;
            }
            else
            {
                mainJob.jobClass.DeliverMaterial().Wait();
                blacklistSites.Clear();
                deliveringMaterial = false;
            }
            FindJob();
        }

        void SetMaterialToDeliver()
        {
            switch (currentNode.node.nodeType)
            {
                case NodeType.BlacksmithSite:
                    materialToDeliver = MaterialType.Ingot;
                    break;
                case NodeType.CarpenterSite:
                    materialToDeliver = MaterialType.Plank;
                    break;
                case NodeType.ForestSite:
                    materialToDeliver = MaterialType.Wood;
                    break;
                case NodeType.MiningSite:
                    materialToDeliver = MaterialType.Ore;
                    break;
            }
        }
    }

    public class Constructor : Agent
    {
        public Constructor(string name) : base(name) { }
        public Constructor(Agent agent) : base(agent) { }

        protected override void FindJob()
        {
            base.FindJob();
        }

        protected override void StartJob()
        {
            base.StartJob();
        }
    }
}