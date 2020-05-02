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

        //protected bool deliveringMaterial = false;

        public Agent(string name)
        {
            this.name = name;
            inventory = new Inventory();
            mainJob = new Job(JobName.Blacksmith, new BlackSmith(inventory, Form1.inst.GetSite(JobName.Blacksmith)), 5);
        }

        //Used to reassign the agents classes
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

        //Initialize the agent when user presses start 
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
            if (blackListJobs.Contains(mainJob.jobName))
            {
                if (subJobs == null || subJobs.Equals(new List<Job>()))
                {   //if the agent does not have any sub jobs 
                    WaitForSpace().Wait();
                    return;
                }

                Job[] sortedSub = subJobs.OrderByDescending(o => o.skillLevel).ToArray();
                foreach (var job in sortedSub)
                    if (!blackListJobs.Contains(job.jobName))
                    {
                        Site jobSite = Form1.inst.GetSite(job.jobName);
                        targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                        currentJob = job;
                        break;
                    }
            }
            else  //assign the agent's job to its main job
            {
                Site jobSite = Form1.inst.GetSite(mainJob.jobName);
                targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                currentJob = mainJob;
            }

            PathFinding();
        }

        async Task WaitForSpace()
        {
            await Task.Run(() => { while (!mainJob.jobClass.jobSite.HasSpace()) { } });
            StartJob();
        }

        //Checks the conditions to do the job before doing it
        protected virtual void StartJob()
        {
            if (currentJob.jobClass.SpaceForAgentMaterial() && currentJob.jobClass.HasEnoughMaterial())
            {
                Task.Run(currentJob.jobClass.ProgressJob).Wait();
                blackListJobs.Clear();
                FindJob();
                return;
            }

            blackListJobs.Add(currentJob.jobName);
            if (blackListJobs.Count == subJobs.Length + 1) //+1 is the mainJob
                blackListJobs.Clear();
            FindJob();
        }

        #region Path Finding
        //Checks whether the agent is already at the targetSite, has already created a found to the targetSite or will create a new path to the targetSite
        protected void PathFinding()
        {
            //If agent is already at the target site
            if (currentNode.node == targetSite.nodeTarget)
            {
                StartJob();
                return;
            }

            currentPath = new Path();
            foreach (var path in sitePaths)
                if (path.start == currentNode.node && path.end == targetSite.nodeTarget)
                    currentPath = path;

            if (currentPath.Equals(new Path()))
            {   //creating a new path if there isn't one
                Path newPath = new Path(currentNode.node, targetSite.nodeTarget, new List<Node>());
                newPath.nodes.Add(currentNode.node);
                currentPath = newPath;
                FindNewPath();
                return;
            }

            TravelExistingPath();
        }

        //using the existing path to travel to the target site
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
            StartJob();
        }

        //recursive function to find a path to the target site based on the direction/angle to the site
        void FindNewPath()
        {
            //get a neighbour node to travel to
            Node closestNode = GetClosestNodeFromAngle();
            Point destPoint = Form1.inst.GetNodeLocation(closestNode.nodeType);
            currentDestination = new Destination<Node>(closestNode, destPoint);

            Task.Run(MoveToDestination).Wait();

            //assign currentNode to the neighbour node
            currentNode = new CurrentNode(closestNode, destPoint);
            currentPath.nodes.Add(closestNode);
            if (currentNode.node.nodeType == targetSite.nodeTarget.nodeType)
            {
                sitePaths.Add(currentPath);
                StartJob();
                return;
            }
            //checking if currentNode is a site and creating a path to the site from start
            if (Form1.inst.CurrentNodeIsSite(currentNode.node.nodeType))
                ComparePaths(closestNode);

            FindNewPath();
        }

        //compares the current path and the existing path to a site to see which has a cheaper cost
        void ComparePaths(Node closestNode)
        {
            Path existingPath = new Path();
            foreach (var path in sitePaths)
                if (path.start == currentPath.start && path.end == closestNode)
                    existingPath = path;

            Path nodePath;
            if (existingPath.Equals(new Path()))
            {   //if an existing path does not exist, then add the current path to the list
                nodePath = currentPath;
                nodePath.end = closestNode;
                sitePaths.Add(nodePath);
                return;
            }

            //compare the path's cost to see if new path is cheaper
            if (PathCost(currentPath) > PathCost(existingPath))
                return;
            //replace existing path with new path
            nodePath = currentPath;
            nodePath.end = closestNode;
            existingPath = nodePath;
        }

        //returns the total cost of the path
        int PathCost(Path path)
        {
            int totalCost = 0;
            for (int i = 0; i < path.nodes.Count - 1; i++)
                totalCost += Form1.inst.GetEdgeCost(path.nodes[i], path.nodes[i + 1]);
            return totalCost;
        }

        //Returns the closest node from currentNode to targetSite based on angle
        Node GetClosestNodeFromAngle()
        {
            double angleToTargetSite = AngleToNode(targetSite.targetPosition);
            double leftAngle = angleToTargetSite - (viewAngle / 2);
            double rightAngle = angleToTargetSite + (viewAngle / 2);

            //checking all connected nodes to see if they are within view angle 
            List<Edge> inViewEdges = new List<Edge>();
            Edge[] connectedEdges = Form1.inst.GetConnectedEdges(currentNode.node);
            foreach (Edge edge in connectedEdges)
            {
                //checking if the connected edge has already been visited before checking of the edge is within view
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

            //loop through the edges within view and find the cheapest edge
            Edge shortestEdge = null;
            if (inViewEdges.Count > 0)
            {
                shortestEdge = inViewEdges.First();
                foreach (Edge edge in inViewEdges)
                    if (edge.cost < shortestEdge.cost)
                        shortestEdge = edge;
            }
            else //if the list was empty, loop through all the edges and find the closest angle to the target site
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
        //display the agent's icon on the map moving towards each node
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
                //the -7 is to centralize the agent's icon since it's pivit point is on the top left corner
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
                return new Point(agentIcon.Location.X, agentIcon.Location.Y);
            }
            set
            {
                if (agentIcon.InvokeRequired) agentIcon.Invoke(new Action(() => { agentIcon.Location = value; }));
                else agentIcon.Location = value;
            }
        }

        //Reference https://stackoverflow.com/a/42070816
        double AngleToNode(Point target)
        {
            double x = target.X - currentNode.position.X;
            double y = target.Y - currentNode.position.Y;

            double rads = Math.Atan2(y, x);
            if (y < 0) rads = Math.PI * 2 + rads;
            return rads * 180 / Math.PI;
        }

        //display the agent's information on a new form
        public virtual void DisplayAgentInformation(object sender, EventArgs e)
        {
            if (!Form1.inst.AgentFormAlreadyOpened(GetHashCode().ToString()))
            {   //assign the form's name to the agent's unqiue hashcode so that the User can not open it twice.
                Form2 form2 = new Form2(this, Form1.inst.GetAgentIndex(this));
                form2.Name = $"Form2_{GetHashCode()}";
                form2.Show();
            }
        }

        //Change the agent box's colour when it is selected and deselected
        public void AgentSelect(object sender, EventArgs e)
        {
            if (!Form1.inst.AgentAlreadySelected(this))
            {   //selecting this agent
                Form1.inst.AgentSelected(this);
                listBox.agentBox.BackColor = Color.FromArgb(49, 113, 170);
                return;
            }
            //deselecting this agent
            Form1.inst.AgentSelected(null);
            listBox.agentBox.BackColor = SystemColors.Control;
        }
    }

    public class Transporter : Agent
    {
        MaterialType materialToDeliver;
        public MaterialType MaterialToDeliver
        {   //created a get set to optimize on the assigning of materialToDeliver in the mainJob.jobClass
            get => materialToDeliver; set
            {
                mainJob.jobClass.MaterialToDeliver = value;
                materialToDeliver = value;
            }
        }
        bool deliveringMaterial = false;

        //similar to the blackListJobs, this list is used so that the agent does not go back to the same site
        List<NodeType> blacklistSites = new List<NodeType>();


        public Transporter(Agent agent) : base(agent)
        {
            //reassign the event where the form will display the agent class's data to Transporter class's data
            listBox.agentBox.DoubleClick -= agent.DisplayAgentInformation;
            listBox.agentBox.DoubleClick += DisplayAgentInformation;

            mainJob = new Job(JobName.Transporter, new Delivery(inventory, Form1.inst.GetSite(JobName.Transporter)), agent.MainJob.skillLevel);
        }

        public override void InitAgent()
        {
            agentIcon = Form1.inst.CreateAgentIcon();

            blacklistSites = new List<NodeType>();
            SetTargetSite(NodeType.StorageSite);
            currentNode = new CurrentNode(mainJob.jobClass.jobSite, Form1.inst.GetNodeLocation(mainJob.SiteNodeType));
            FindJob();
        }

        //assigning the target site for the agent to go to
        protected override void FindJob()
        {
            if (deliveringMaterial)
            {   //checking which site the agent is at and go towards its target site
                NodeType currentNodeType = currentNode.node.nodeType;
                switch (currentNodeType)
                {   //if agent is at storage site, check which material the agent is delivering
                    case NodeType.StorageSite:
                        switch (MaterialToDeliver)
                        {
                            case MaterialType.Wood:
                                SetTargetSite(NodeType.CarpenterSite);
                                break;
                            case MaterialType.Ore:
                                SetTargetSite(NodeType.BlacksmithSite);
                                break;
                            case MaterialType.Plank:
                            case MaterialType.Ingot:
                                SetTargetSite(NodeType.MainSite);
                                break;
                        }
                        break;
                    case NodeType.BlacksmithSite:
                    case NodeType.CarpenterSite:
                        SetTargetSite(NodeType.StorageSite);
                        break;
                    case NodeType.ForestSite:
                        SetTargetSite(NodeType.CarpenterSite);
                        break;
                    case NodeType.MiningSite:
                        SetTargetSite(NodeType.BlacksmithSite);
                        break;
                }
            }
            else if (blacklistSites.Contains(currentNode.node.nodeType))
            {
                //go to the first available site which has not been blacklisted
                foreach (Site site in Form1.inst.TakeOutSites)
                    if (!blacklistSites.Contains(site.nodeType))
                    {
                        SetTargetSite(site.nodeType);
                        break;
                    }

                //if all sites have been blacklisted
                if (blacklistSites.Count == Form1.inst.TakeOutSites.Length)
                {   //clear the list and go back to storage site
                    blacklistSites.Clear();
                    SetTargetSite(NodeType.StorageSite);
                }
            }
            PathFinding();
        }

        //sets the target of which the agent will travel towards and changing the jobClass's site data to the target site's
        void SetTargetSite(NodeType type)
        {
            mainJob.jobClass.jobSite = Form1.inst.GetSite(type);
            targetSite = new Destination<Site>(mainJob.jobClass.jobSite, Form1.inst.GetNodeLocation(type));
        }

        //Checks if the agent is at the site to deliver or take out materials
        protected override void StartJob()
        {
            if (!deliveringMaterial)
            {
                if (mainJob.SiteNodeType == NodeType.MainSite) //if agent is at the main site then go back to the storage site
                    SetTargetSite(NodeType.StorageSite);
                else if (CanSelectMaterial())
                {
                    Task.Run(mainJob.jobClass.TakeOutMaterial).Wait();
                    blacklistSites.Add(currentNode.node.nodeType);
                    deliveringMaterial = true;
                }
            }
            else
            {
                Task.Run(mainJob.jobClass.DeliverMaterial).Wait();
                deliveringMaterial = false;
            }
            FindJob();
        }

        //returns true if the site has space and materials, else return false and blacklist the site
        bool CanSelectMaterial()
        {
            if (mainJob.SiteNodeType != NodeType.StorageSite && mainJob.jobClass.HasEnoughMaterial())
            {
                switch (currentNode.node.nodeType)
                {
                    case NodeType.BlacksmithSite:
                        MaterialToDeliver = MaterialType.Ingot;
                        break;
                    case NodeType.CarpenterSite:
                        MaterialToDeliver = MaterialType.Plank;
                        break;
                    case NodeType.ForestSite:
                        MaterialToDeliver = MaterialType.Wood;
                        break;
                    case NodeType.MiningSite:
                        MaterialToDeliver = MaterialType.Ore;
                        break;
                }
            }
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
                    return false;
                }

                MaterialToDeliver = material.materialType;
            }
            else //if site does not have enough materials to take out and site is not storage site
            {
                blacklistSites.Add(currentNode.node.nodeType);
                return false;
            }
            return true;
        }

        public override void DisplayAgentInformation(object sender, EventArgs e)
        {
            if (!Form1.inst.AgentFormAlreadyOpened(GetHashCode().ToString()))
            {   //assign the form's name to the agent's unqiue hashcode so that the User can not open it twice.
                Form2 form2 = new Form2(this, Form1.inst.GetAgentIndex(this));
                form2.Name = $"Form2_{GetHashCode()}";
                form2.Show();
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