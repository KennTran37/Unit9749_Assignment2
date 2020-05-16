using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    public class Agent
    {
        public string name = null;
        const float moveSpeed = 10f; // meters per second
        //slowest speed must be 4, any value lower then that then the calculations will be broken.
        //this is because Point can only take in Integers and not decimal values

        protected Job currentJob;
        public Job CurrentJob
        {
            set
            {
                currentJob = value.Equals(new Job()) ? mainJob : value;
                if (listBox.progressJob != null)
                    listBox.progressJob.Image = IconPath.GetIcon(currentJob.jobName);
                if (form2CurrentJobIcon != null)
                    form2CurrentJobIcon.Image = IconPath.GetIcon(currentJob.jobName);
            }
        }

        public JobName CurrentJobName => currentJob.jobName;
        public Job mainJob { get; set; }
        public Job[] subJobs { get; set; }

        protected bool deliveringMaterial = false;

        //this list holds jobs that the agent could not do and prevent the agent from going back to the same job after going to another job
        protected List<JobName> blackListJobs = new List<JobName>();

        Path currentPath;
        List<Path> sitePaths = new List<Path>();
        protected CurrentNode currentNode;
        protected Destination<Site> targetSite;
        Destination<Node> currentDestination;

        protected Inventory inventory;
        public Inventory Inventory { get => inventory; set => inventory = value; }

        public PictureBox agentIcon; //used to display the location of the agent when they are moving on the map
        public AgentListBox listBox;
        PictureBox form2CurrentJobIcon;
        ProgressBar form2currentJobBar;
        public ProgressBar form3Bar;

        public delegate void UpdateProgress(int value);
        protected UpdateProgress updateProgressHandler;

        //used to determine which node to take that is within the angle
        const double viewAngle = 50;

        //protected bool deliveringMaterial = false;

        public Agent(string name)
        {
            this.name = name;
            inventory = new Inventory(0, 10);
            mainJob = new Job(JobName.Blacksmith, new BlackSmith(inventory, Form1.inst.GetSite(JobName.Blacksmith)), 5);
            CurrentJob = mainJob;
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
            form3Bar = agent.form3Bar;

            listBox.agentBox.DoubleClick -= agent.DisplayAgentInformation;
            listBox.agentBox.DoubleClick += DisplayAgentInformation;
            CurrentJob = mainJob;
        }

        //Initialize the agent when user presses start 
        public virtual void InitAgent()
        {
            agentIcon = Form1.inst.CreateAgentIcon();

            blackListJobs = new List<JobName>();
            currentNode = new CurrentNode(mainJob.jobClass.jobSite, Form1.inst.GetNodeLocation(mainJob.SiteNodeType));
            CurrentJob = mainJob;

            updateProgressHandler += UpdateProgressBars;
            FindJob();
        }

        public void UpdateProgressBars(int value)
        {
            if (form2currentJobBar != null)
            {
                if (form2currentJobBar.InvokeRequired)
                    form2currentJobBar.Invoke(new Action(() => { form2currentJobBar.Value = value; }));
                else form2currentJobBar.Value = value;
            }

            if (form3Bar != null)
            {
                if (form3Bar.InvokeRequired)
                    form3Bar.Invoke(new Action(() => { form3Bar.Value = value; }));
                else form3Bar.Value = value;
            }

            if (listBox.progressBar.InvokeRequired)
                listBox.progressBar.Invoke(new Action(() => { listBox.progressBar.Value = value; }));
            else listBox.progressBar.Value = value;
        }

        //Finding a job the agent can do based on their skill level if agent can't do its main job
        protected virtual void FindJob()
        {
            if (deliveringMaterial)
                SetDeliverySite();
            else if (blackListJobs.Contains(mainJob.jobName))
            {   //if agent can't do main job search through the sub jobs
                //sort the subJobs by descending order of the jobs' skillLevel
                Job[] sortedSub = (from job in subJobs orderby job.skillLevel descending select job).ToArray();
                //loop through the array to find the first job that isn't in the black list and assign the currentJob to that job
                foreach (var job in sortedSub)
                    if (!blackListJobs.Contains(job.jobName))
                    {   
                        Site jobSite = Form1.inst.GetSite(job.jobName);
                        targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                        CurrentJob = job;
                        break;
                    }
            }
            else  //assign the agent's job to its main job
            {
                Site jobSite = Form1.inst.GetSite(mainJob.jobName);
                targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                CurrentJob = mainJob;
            }
            PathFinding();
        }

        //Used when the agent does not have any sub jobs and is waiting for space in it's main job's site
        protected async Task WaitForSpace()
        {
            try
            {
                await Task.Run(() => { while (!this.mainJob.jobClass.jobSite.HasSpace() && !this.mainJob.jobClass.HasEnoughMaterial()) { } }, Form1.inst.cts.Token);
                StartJob();
            }
            catch (Exception) { }
        }

        protected void BlackListCurrentJob()
        {
            blackListJobs.Add(currentJob.jobName);
            if (blackListJobs.Count == subJobs.Length + 1) //+1 is the mainJob
                blackListJobs.Clear();
        }

        //Checks the conditions on what job the agent is doing and if the agent can do it
        protected virtual void StartJob()
        {
            try
            {
                if (currentJob.jobName == JobName.Transporter)
                    DeliveryJob();
                else if (currentJob.jobClass.SpaceForAgentMaterial() && currentJob.jobClass.HasEnoughMaterial())
                {   //checking if site has space for agent and materials and checking if the site/agent has enough materials to craft
                    currentJob.jobClass.jobSite.AddAgent(this);     //add agent to site's currentAgent list
                    Task.Run(() => currentJob.jobClass.ProgressJob(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                    currentJob.jobClass.jobSite.RemoveAgent(this);
                    blackListJobs.Clear();
                }
                else
                {
                    //if the agent does not have any sub jobs 
                    if (subJobs == null)
                        WaitForSpace().Wait();
                    else
                        BlackListCurrentJob();
                }
                FindJob();
            }
            catch (Exception) { }
        }

        //Set the targetSite where the agent will deliver the materials to
        protected void SetDeliverySite()
        {
            switch (currentJob.jobClass.MaterialToDeliver)
            {
                case MaterialType.Plank:
                case MaterialType.Ingot:
                    SetTargetSite(NodeType.MainSite);
                    break;
                case MaterialType.Wood:
                    SetTargetSite(NodeType.CarpenterSite);
                    break;
                case MaterialType.Ore:
                    SetTargetSite(NodeType.BlacksmithSite);
                    break;
            }
        }

        //used when the agent has transporter as a subjob
        //the agent will check the storage for the material with the most amount and then take out it to deliver
        protected void DeliveryJob()
        {
            if (AgentHasMaterials())
                return;

            //if site has space for agent
            if (currentJob.jobClass.SpaceForAgentMaterial())
            {
                currentJob.jobClass.jobSite.AddAgent(this);
                if (!deliveringMaterial && StorageHasMaterial(out MaterialBox box))
                {   //taking out materials from the storage site
                    currentJob.jobClass.MaterialToDeliver = box.materialType;
                    Task.Run(() => currentJob.jobClass.TakeOutMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                    deliveringMaterial = true;
                }
                else if (deliveringMaterial)
                {   //delivering the materials to the target site
                    Task.Run(() => currentJob.jobClass.DeliverMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                    //set the jobSite back to it's default site which is StorageSite for Transporters
                    currentJob.jobClass.jobSite = Form1.inst.GetSite(NodeType.StorageSite);
                    deliveringMaterial = false;
                    blackListJobs.Clear();
                }
                currentJob.jobClass.jobSite.RemoveAgent(this);
            }
        }

        //searches for the material with the highest amount inside agent's inventory
        //and sets it as the material to deliver
        bool AgentHasMaterials()
        {
            MaterialBox material = new MaterialBox();
            foreach (var mat in AgentInventory())
                if (mat.HasAmount(currentJob.jobClass.TakeOutAmount))
                    if (material.Current < mat.Current)
                        material = mat;
            if (material.Equals(new MaterialBox()))
                return false;

            currentJob.jobClass.MaterialToDeliver = material.materialType;
            return false;
        }

        //sets the target of which the agent will travel towards and changing the jobClass's site data to the target site's
        protected virtual void SetTargetSite(NodeType type)
        {
            currentJob.jobClass.jobSite = Form1.inst.GetSite(type);
            targetSite = new Destination<Site>(currentJob.jobClass.jobSite, Form1.inst.GetNodeLocation(type));
        }

        //searches for the material with the highest amount inside storage site
        //and sets it has the material to deliver
        protected bool StorageHasMaterial(out MaterialBox material)
        {
            material = new MaterialBox();
            foreach (var mat in SiteInventory())
                if (mat.HasAmount(mainJob.jobClass.TakeOutAmount))
                    if (material.Current < mat.Current)
                        material = mat;
            //if a material box was not selected
            if (material.Equals(new MaterialBox()))
                return false;
            return true;
        }

        IEnumerable<MaterialBox> SiteInventory()
        {
            yield return currentJob.SitePlank;
            yield return currentJob.SiteIngot;
            yield return currentJob.SiteWood;
            yield return currentJob.SiteOre;
        }

        IEnumerable<MaterialBox> AgentInventory()
        {
            yield return inventory.plank;
            yield return inventory.ingot;
            yield return inventory.wood;
            yield return inventory.ore;
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

            //Reset the currentPath and loop through the sitePaths list to find a matching path
            currentPath = new Path();
            foreach (var path in sitePaths)
                if (path.start == currentNode.node && path.end == targetSite.nodeTarget)
                    currentPath = path;

            if (currentPath.Equals(new Path()))
            {   //creating a new path if there isn't one
                currentPath = new Path(currentNode.node, targetSite.nodeTarget, new List<Node>());
                currentPath.nodes.Add(currentNode.node);
                FindNewPath();
                return;
            }

            TravelExistingPath();
        }

        //using the existing path to travel to the target site
        void TravelExistingPath()
        {
            try
            {
                List<Node> path = currentPath.nodes;
                for (int i = 0; i < path.Count; i++)
                {   //loop through each node and get it's position to travel to
                    Point destPoint = Form1.inst.GetNodeLocation(path[i].nodeType);
                    currentDestination = new Destination<Node>(path[i], destPoint);
                    Task.Run(MoveToDestination).Wait(Form1.inst.cts.Token);
                    //when finished traveling, set the currentNode to the node
                    currentNode = new CurrentNode(path[i], destPoint);
                }
                StartJob();
            }
            catch (Exception) { ShowAgentIcon(false); }
        }

        //recursive function to find a path to the target site based on the direction/angle to the site
        void FindNewPath()
        {
            try
            {
                //get a neighbour node to travel to
                Node closestNode = GetClosestNodeFromAngle();
                Point destPoint = Form1.inst.GetNodeLocation(closestNode.nodeType);
                currentDestination = new Destination<Node>(closestNode, destPoint);

                Task.Run(MoveToDestination).Wait(Form1.inst.cts.Token);

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
            catch (Exception) { ShowAgentIcon(false); }
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
                    //if the other point is the targetSite
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

            return GetShortestEdge(inViewEdges, connectedEdges, angleToTargetSite).GetOtherPoint(currentNode.node);
        }

        //Finding the shortest node if there are edges in inViewEdges list or find the closest edge by angle in connectedEdges array
        Edge GetShortestEdge(List<Edge> inViewEdges, Edge[] connectedEdges, double angleToTargetSite)
        {
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
                        //subtracting the angleToPoint and closestAngle by angleToTargetSite to find which has a smaller value
                        //the smaller the value, the closer the angle is to the angleToTargetSite
                        if (Math.Abs(angleToTargetSite - angleToPoint) < Math.Abs(angleToTargetSite - closestAngle))
                        {
                            closestAngle = angleToPoint;
                            shortestEdge = edge;
                        }
                    }
                }
            }
            return shortestEdge;
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
        #endregion

        //display the agent's information on a new form
        public virtual void DisplayAgentInformation(object sender, EventArgs e)
        {
            if (!Form1.inst.AgentFormAlreadyOpened(GetHashCode().ToString()))
            {   //assign the form's name to the agent's unqiue hashcode so that the User can not open it twice.
                Form2 form2 = new Form2(this, Form1.inst.GetAgentIndex(this));
                form2.Name = $"Form2_{GetHashCode()}";
                form2.Show();
                form2currentJobBar = form2.currentJobProgressBar;
                form2CurrentJobIcon = form2.currentJobIcon;
                listBox.progressJob.Image = form2CurrentJobIcon.Image = IconPath.GetIcon(currentJob.jobName);
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
}