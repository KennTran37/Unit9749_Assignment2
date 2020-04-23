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
        const float moveSpeed = 4f; // meters per second

        public CurrentJob currentJob;
        public JobInfomation mainJob;
        public JobInfomation[] subJobs;

        //this list holds jobs that the agent could not do and prevent the agent from going back to the same job after going to another job
        public List<Job> visitedJobsList = new List<Job>();

        Dictionary<NodeType, Path> sitePaths = new Dictionary<NodeType, Path>();
        KeyValuePair<NodeType, Path> currentPath = new KeyValuePair<NodeType, Path>();
        public CurrentNode currentNode;
        public Destination<Site> targetSite;
        public Destination<Node> currentDestination;

        public Inventory inventory;

        public PictureBox agentIcon;
        public AgentListBox listBox;
        public ProgressBar siteProgressBar;

        //used to determine which node to take that is within the angle
        const double viewAngle = 50;

        public Agent(string name, JobInfomation mainJob)
        {
            this.name = name;
            this.mainJob = mainJob;

            inventory = new Inventory(new MaterialBox(null, null, null, 0, 10));
        }

        public void InitAgent()
        {
            agentIcon = Form1.inst.CreateAgentIcon();

            visitedJobsList = new List<Job>();
            Node jobSite = Form1.inst.GetNodeByJob(mainJob.jobType);
            if (jobSite != null)   //adding this checker incase it returns StorageSite which is the default returner
            {
                currentNode = new CurrentNode(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                currentJob.job = mainJob.jobType;
                currentJob.site = Form1.inst.GetSiteByNodeType(currentNode.node.nodeType);
                FindJob();
            }
        }

        void FindJob()
        {
            if (visitedJobsList.Contains(mainJob.jobType) && subJobs != null)
            {
                JobInfomation[] sortedSub = subJobs.OrderBy(o => o.skillLevel).ToArray();
                foreach (var job in sortedSub)
                    if (!visitedJobsList.Contains(job.jobType))
                    {
                        Site jobSite = Form1.inst.GetNodeByJob(job.jobType);
                        if (jobSite.nodeType != NodeType.StorageSite)
                        {
                            targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                            currentJob.job = job.jobType;
                            break;
                        }
                    }
            }
            else
            {
                Site jobSite = Form1.inst.GetNodeByJob(mainJob.jobType);
                if (jobSite.nodeType != NodeType.StorageSite)
                {
                    targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                    currentJob.job = mainJob.jobType;
                }
            }

            PathFinding();
        }

        #region Job Progression
        public void ProgressJob()
        {
            //Task.Run(PutInMaterials).Wait();

            currentJob.site = Form1.inst.GetSiteByNodeType(targetSite.nodeTarget.nodeType);
            if (currentJob.site.HasSpace())
            {
                if (!HasSpaceForMaterials())
                {
                    TakeOutMaterials().Wait();
                    //Take Materials to Storage Site
                }
                else
                {
                    if (currentJob.job == Job.Blacksmith || currentJob.job == Job.Carpenter)
                        Task.Run(CraftingJob).Wait();
                    else if (currentJob.job == Job.Logger || currentJob.job == Job.Miner)
                        Task.Run(GatheringJob).Wait();
                    else if (currentJob.job == Job.Transporter)
                        Task.Run(TakeOutMaterials);
                    else if (currentJob.job == Job.Constructor)
                        Task.Run(ConstructorJob);

                    currentPath = new KeyValuePair<NodeType, Path>();
                    visitedJobsList.Clear();
                    FindJob();
                }
            }
            else
            {
                visitedJobsList.Add(currentJob.job);
                if (visitedJobsList.Count == subJobs.Length + 1) //+1 is the mainJob
                    visitedJobsList.Clear();
                FindJob();
            }
        }

        bool HasSpaceForMaterials()
        {
            if (currentJob.job == Job.Transporter)
                return currentJob.site.inventory.wood.HasSpace() || currentJob.site.inventory.plank.HasSpace() || currentJob.site.inventory.ore.HasSpace() || currentJob.site.inventory.ingot.HasSpace();
            if (currentJob.job == Job.Carpenter)
                return currentJob.site.inventory.wood.HasSpace();
            if (currentJob.job == Job.Blacksmith)
                return currentJob.site.inventory.ore.HasSpace();
            if (currentJob.job == Job.Miner)
                return currentJob.site.inventory.ore.HasSpace();
            if (currentJob.job == Job.Logger)
                return currentJob.site.inventory.wood.HasSpace();
            if (currentJob.job == Job.Constructor)
                return currentJob.site.inventory.plank.HasAmount(5) && currentJob.site.inventory.ingot.HasAmount(5);
            return false;
        }

        async Task CraftingJob()
        {
            if (currentJob.job == Job.Blacksmith)
                currentJob.site.inventory.ore.Current -= 5;
            if (currentJob.job == Job.Carpenter)
                currentJob.site.inventory.wood.Current -= 5;
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(1000);
                if (currentJob.job == Job.Blacksmith)
                    if (!currentJob.site.inventory.ingot.TryPutInMaterial())
                        inventory.ingot.TryPutInMaterial();

                if (currentJob.job == Job.Carpenter)
                    if (!currentJob.site.inventory.plank.TryPutInMaterial())
                        inventory.plank.TryPutInMaterial();
                Form1.inst.SetLabelAngle("Crafting: " + i.ToString());
            }
            Form1.inst.SetLabelAngle("Comeplete Crafting");
        }

        async Task GatheringJob()
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(500);
                if (currentJob.job == Job.Miner)
                    if (!currentJob.site.inventory.ore.TryPutInMaterial())
                        inventory.ore.TryPutInMaterial();

                if (currentJob.job == Job.Logger)
                    if (!currentJob.site.inventory.wood.TryPutInMaterial())
                        inventory.wood.TryPutInMaterial();
                Form1.inst.SetLabelAngle("Gathering: " + i.ToString());
            }
            Form1.inst.SetLabelAngle("Comeplete Gathering");
        }

        async Task TakeOutMaterials()
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                if (currentJob.site.nodeType == (NodeType.BlacksmithSite | NodeType.MiningSite | NodeType.StorageSite))
                    TryTakeOutMat(currentJob.site.inventory.ore, inventory.ore);
                if (currentJob.site.nodeType == (NodeType.CarpenterSite | NodeType.ForestSite | NodeType.StorageSite))
                    TryTakeOutMat(currentJob.site.inventory.wood, inventory.wood);
                if (currentJob.site.nodeType == (NodeType.StorageSite | NodeType.MainSite))
                {
                    TryTakeOutMat(currentJob.site.inventory.ingot, inventory.ingot);
                    TryTakeOutMat(currentJob.site.inventory.plank, inventory.plank);
                }
            }
        }

        void TryTakeOutMat(MaterialBox siteMat, MaterialBox agentMat)
        {
            if (!siteMat.HasAmount(0) || !agentMat.HasSpace())
                return;
            agentMat.Current++;
            siteMat.Current--;
        }

        async Task PutInMaterials()
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                Form1.inst.SetLabelAngle("Put in: " + i.ToString());
                if (currentJob.site.nodeType == (NodeType.BlacksmithSite | NodeType.MiningSite | NodeType.StorageSite))
                    TryPutInMat(currentJob.site.inventory.ore, inventory.ore);
                if (currentJob.site.nodeType == (NodeType.CarpenterSite | NodeType.ForestSite | NodeType.StorageSite))
                    TryPutInMat(currentJob.site.inventory.wood, inventory.wood);
                if (currentJob.site.nodeType == (NodeType.StorageSite | NodeType.MainSite))
                {
                    TryPutInMat(currentJob.site.inventory.ingot, inventory.ingot);
                    TryPutInMat(currentJob.site.inventory.plank, inventory.plank);
                }
            }
            Form1.inst.SetLabelAngle("Complete Putting In Material");
        }

        void TryPutInMat(MaterialBox siteMat, MaterialBox agentMat)
        {
            if (!siteMat.HasSpace() || !agentMat.HasAmount(0))
                return;
            agentMat.Current--;
            siteMat.Current++;
        }

        async Task ConstructorJob()
        {
            await Task.Delay(1);
        }
        #endregion

        #region Path Finding
        void PathFinding()
        {
            foreach (var path in sitePaths)
                if (path.Key == currentNode.node.nodeType && path.Value.destination.nodeType == targetSite.nodeTarget.nodeType)
                    currentPath = path;

            if (currentNode.node == targetSite.nodeTarget)
                ProgressJob();
            else
            {
                if (!currentPath.Equals(new KeyValuePair<NodeType, Path>()))
                {
                    Form1.inst.SetLabelAngle("Use Existing Path");
                    TravelExistingPath();
                }
                else
                {
                    Path newPath = new Path(targetSite.nodeTarget, new List<Node>());
                    newPath.nodes.Add(currentNode.node);
                    currentPath = new KeyValuePair<NodeType, Path>(currentNode.node.nodeType, newPath);
                    Form1.inst.SetLabelAngle("Travel New Path");
                    FindNewPath();
                }
            }
        }

        void TravelExistingPath()
        {
            List<Node> path = currentPath.Value.nodes;
            for (int i = 0; i < path.Count; i++)
            {
                Point destPoint = Form1.inst.GetNodeLocation(path[i].nodeType);
                currentDestination = new Destination<Node>(path[i], destPoint);
                Task.Run(MoveToDestination).Wait();

                currentNode = new CurrentNode(path[i], destPoint);
            }
            Form1.inst.SetLabelAngle("Existing Path Complete");
            ProgressJob();
        }

        void FindNewPath()
        {
            Node closestNode = GetClosestNodeFromAngle();
            Point destPoint = Form1.inst.GetNodeLocation(closestNode.nodeType);
            currentDestination = new Destination<Node>(closestNode, destPoint);

            Task.Run(MoveToDestination).Wait();

            currentNode = new CurrentNode(closestNode, destPoint);
            currentPath.Value.nodes.Add(closestNode);
            if (currentNode.node.nodeType == targetSite.nodeTarget.nodeType)
            {
                Form1.inst.SetLabelAngle("New Path Complete");
                sitePaths.Add(currentPath.Key, currentPath.Value);
                ProgressJob();
                return;
            }
            if (Form1.inst.CurrentNodeIsSite(currentNode.node.nodeType))
            {
                KeyValuePair<NodeType, Path> existingPath = new KeyValuePair<NodeType, Path>();
                foreach (var path in sitePaths)
                    if (path.Key == currentPath.Key && path.Value.destination == closestNode)
                        existingPath = path;

                if (!existingPath.Equals(new KeyValuePair<NodeType, Path>()))
                {
                    if (PathCost(currentPath.Value) < PathCost(existingPath.Value))
                    {
                        Path nodePath = currentPath.Value;
                        nodePath.destination = closestNode;
                        existingPath = new KeyValuePair<NodeType, Path>(currentPath.Key, nodePath);
                    }
                }
                else
                {
                    Path nodePath = currentPath.Value;
                    nodePath.destination = closestNode;
                    sitePaths.Add(currentPath.Key, nodePath);
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
                Node otherPoint = edge.GetOtherPoint(currentNode.node);
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
                    double angleToPoint = AngleToNode(Form1.inst.GetNodeLocation(otherPoint.nodeType));
                    if (Math.Abs(angleToTargetSite - angleToPoint) < Math.Abs(angleToTargetSite - closestAngle))
                    {
                        closestAngle = angleToPoint;
                        shortestEdge = edge;
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
}