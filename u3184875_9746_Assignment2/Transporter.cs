using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace u3184875_9746_Assignment2
{
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

            mainJob = new Job(JobName.Transporter, new Delivery(inventory, Form1.inst.GetSite(JobName.Transporter)), agent.mainJob.skillLevel);
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
                        if (currentNode.node.nodeType == NodeType.StorageSite)
                            SetTargetSite(NodeType.MainSite);
                        else
                            SetTargetSite(NodeType.StorageSite);
                        break;
                }
                PathFinding();
                return;
            }

            //if all sites have been blacklisted
            if (blacklistSites.Count == Form1.inst.TakeOutSites.Length)
            {   //clear the list and go back to storage site
                blacklistSites.Clear();
                SetTargetSite(NodeType.StorageSite);
            }
            else  //go to the first available site which has not been blacklisted
                SetTargetSite(Form1.inst.TakeOutSites.First(f => !blacklistSites.Contains(f.nodeType)).nodeType);
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
            try
            {
                if (!deliveringMaterial)
                {
                    if (mainJob.SiteNodeType == NodeType.MainSite) //if agent is at the main site then go back to the storage site
                        SetTargetSite(NodeType.StorageSite);
                    else if (CanSelectMaterial())
                    {
                        Task.Run(mainJob.jobClass.TakeOutMaterial).Wait(Form1.inst.cts.Token);
                        //blacklist the site so that the agent doesn't go back to it
                        blacklistSites.Add(currentNode.node.nodeType);
                        deliveringMaterial = true;
                    }
                }
                else
                {
                    Task.Run(mainJob.jobClass.DeliverMaterial).Wait(Form1.inst.cts.Token);
                    deliveringMaterial = false;
                }
                FindJob();
            }
            catch (Exception) { }
        }

        //returns true if the site has space and materials, else return false and blacklist the site
        bool CanSelectMaterial()
        {
            if (mainJob.SiteNodeType != NodeType.StorageSite && mainJob.jobClass.HasEnoughMaterial())
            {   //if site has space for materials and dpending on the site, assign the materials to take out
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
}