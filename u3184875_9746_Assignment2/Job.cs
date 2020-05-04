using System;
using System.Threading.Tasks;

namespace u3184875_9746_Assignment2
{
    public struct Job
    {
        public Job(JobName jobName, JobBase jobClass, int skillLevel)
        {
            this.jobName = jobName;
            this.jobClass = jobClass;
            this.skillLevel = skillLevel;
        }

        public JobName jobName { get; set; }
        public JobBase jobClass { get; set; }
        public int skillLevel { get; set; }
        public NodeType SiteNodeType => jobClass.jobSite.nodeType;
    }


    public abstract class JobBase
    {
        protected Inventory agentInventory;
        public Site jobSite;
        protected Site storageSite;

        protected const int takeOutNumMaterials = 5;
        public int TakeOutAmount => takeOutNumMaterials;
        protected const int putInNumMaterials = 5;
        protected const int collectNumMaterials = 5;
        protected const int jobTimeDelay = 1000; //ms
        protected bool takeMatFromAgentInvent = false;
        public MaterialType MaterialToDeliver { get; set; }

        public JobBase(Inventory agentInventory, Site jobSite)
        {
            this.agentInventory = agentInventory;
            this.jobSite = jobSite;
            storageSite = Form1.inst.GetSite(NodeType.StorageSite);
        }

        public abstract Task ProgressJob();
        //Checks if the site has enough space for the materials that agent will put in
        public abstract bool SpaceForAgentMaterial();
        public virtual Task DeliverMaterial() => null;
        public virtual Task TakeOutMaterial() => null;
        //used by crafters to see if both the site's and agent's inventory has enough materials to craft
        public virtual bool HasEnoughMaterial() => true;
    }

    class BlackSmith : JobBase
    {
        public BlackSmith(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                //if agent should take out materials from inventory is False and site has materials || take out materials from inventory is True and agent's inventory has materials
                if (!takeMatFromAgentInvent && jobSite.inventory.ore.TryTakeOutMaterial() || takeMatFromAgentInvent && agentInventory.ore.TryTakeOutMaterial())
                    if (!jobSite.inventory.ingot.TryPutInMaterial())
                        agentInventory.ingot.TryPutInMaterial();
            }
        }

        public override bool SpaceForAgentMaterial() => jobSite.HasSpace() && jobSite.inventory.ingot.HasSpace();
        public override bool HasEnoughMaterial()
        {
            if (!jobSite.inventory.ore.HasAmount(takeOutNumMaterials))
                return takeMatFromAgentInvent = agentInventory.ore.HasAmount(takeOutNumMaterials);
            return true;
        }
    }

    class Carpenter : JobBase
    {
        public Carpenter(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                //if agent should take out materials from inventory is False and site has materials || take out materials from inventory is True and agent's inventory has materials
                if (!takeMatFromAgentInvent && jobSite.inventory.wood.TryTakeOutMaterial() || takeMatFromAgentInvent && agentInventory.wood.TryTakeOutMaterial())
                    if (!jobSite.inventory.plank.TryPutInMaterial())
                        agentInventory.plank.TryPutInMaterial();
            }
        }

        public override bool SpaceForAgentMaterial() => jobSite.HasSpace() && jobSite.inventory.plank.HasSpace();
        public override bool HasEnoughMaterial()
        {
            if (!jobSite.inventory.wood.HasAmount(takeOutNumMaterials))
                return takeMatFromAgentInvent = (agentInventory.wood.HasAmount(takeOutNumMaterials));
            return true;
        }
    }

    class Logger : JobBase
    {
        public Logger(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < collectNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                //if there isn't any space within the site's inventory
                if (!jobSite.inventory.wood.TryPutInMaterial())
                    agentInventory.wood.TryPutInMaterial(); //put it into the agent's inventory
            }
        }

        public override bool SpaceForAgentMaterial() => jobSite.HasSpace() && jobSite.inventory.wood.HasSpace();
    }

    class Miner : JobBase
    {
        public Miner(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < collectNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                //if there isn't any space within the site's inventory
                if (!jobSite.inventory.ore.TryPutInMaterial())
                    agentInventory.ore.TryPutInMaterial(); //put it into the agent's inventory
            }
        }

        public override bool SpaceForAgentMaterial() => jobSite.HasSpace() && jobSite.inventory.ore.HasSpace();
    }

    class Delivery : JobBase
    {
        public Delivery(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override Task ProgressJob() => null;

        public override bool SpaceForAgentMaterial() => jobSite.HasSpace();

        public override bool HasEnoughMaterial()
        {
            switch (jobSite.nodeType)
            {
                case NodeType.BlacksmithSite:
                    return jobSite.inventory.ingot.HasAmount(takeOutNumMaterials);
                case NodeType.CarpenterSite:
                    return jobSite.inventory.plank.HasAmount(takeOutNumMaterials);
                case NodeType.ForestSite:
                    return jobSite.inventory.wood.HasAmount(takeOutNumMaterials);
                case NodeType.MiningSite:
                    return jobSite.inventory.ore.HasAmount(takeOutNumMaterials);
            }
            return true;
        }

        public override async Task TakeOutMaterial()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                switch (MaterialToDeliver)
                {
                    case MaterialType.Wood:
                        if (jobSite.inventory.wood.TryTakeOutMaterial())
                            agentInventory.wood.TryPutInMaterial();
                        break;
                    case MaterialType.Plank:
                        if (jobSite.inventory.plank.TryTakeOutMaterial())
                            agentInventory.plank.TryPutInMaterial();
                        break;
                    case MaterialType.Ore:
                        if (jobSite.inventory.ore.TryTakeOutMaterial())
                            agentInventory.ore.TryPutInMaterial();
                        break;
                    case MaterialType.Ingot:
                        if (jobSite.inventory.ingot.TryTakeOutMaterial())
                            agentInventory.ingot.TryPutInMaterial();
                        break;
                }
            }
        }

        public override async Task DeliverMaterial()
        {
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                switch (MaterialToDeliver)
                {
                    case MaterialType.Wood:
                        if (agentInventory.wood.TryTakeOutMaterial())
                            jobSite.inventory.wood.TryPutInMaterial();
                        break;
                    case MaterialType.Plank:
                        if (agentInventory.plank.TryTakeOutMaterial())
                            jobSite.inventory.plank.TryPutInMaterial();
                        break;
                    case MaterialType.Ore:
                        if (agentInventory.ore.TryTakeOutMaterial())
                            jobSite.inventory.ore.TryPutInMaterial();
                        break;
                    case MaterialType.Ingot:
                        if (agentInventory.ingot.TryTakeOutMaterial())
                            jobSite.inventory.ingot.TryPutInMaterial();
                        break;
                }
            }
        }
    }

    class Builder : JobBase
    {
        public Builder(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override Task ProgressJob()
        {
            throw new NotImplementedException();
        }

        public override bool SpaceForAgentMaterial()
        {
            throw new NotImplementedException();
        }

        public override Task TakeOutMaterial()
        {
            throw new NotImplementedException();
        }

        public override Task DeliverMaterial()
        {
            throw new NotImplementedException();
        }
    }
}