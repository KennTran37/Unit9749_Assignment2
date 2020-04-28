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

        public JobBase(Inventory agentInventory, Site jobSite)
        {
            this.agentInventory = agentInventory;
            this.jobSite = jobSite;
            storageSite = Form1.inst.GetSiteByNodeType(NodeType.StorageSite);
        }

        public abstract Task ProgressJob();
        //Checks if the site has enough space for the materials that agent will put in
        public abstract bool HasSpaceForMaterial();
        public abstract Task DeliverMaterial();
        public abstract Task TakeOutMaterial();
        //used by crafters to see if both the site's and agent's inventory has enough materials to craft
        public virtual Task TakeOutMaterial(MaterialType type) => null;
        public virtual bool HasEnoughMaterial() => true;
    }

    class BlackSmith : JobBase
    {
        public BlackSmith(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                Form1.inst.SetLabelAngle($"Task: {i}");
                await Task.Delay(jobTimeDelay);
                if (!takeMatFromAgentInvent && jobSite.inventory.ore.TryTakeOutMaterial() || takeMatFromAgentInvent && agentInventory.ore.TryTakeOutMaterial())
                    if (!jobSite.inventory.ingot.TryPutInMaterial())
                        agentInventory.ingot.TryPutInMaterial();
            }
        }

        public override bool HasSpaceForMaterial() => jobSite.inventory.ingot.HasSpace();
        public override bool HasEnoughMaterial()
        {
            if (jobSite.inventory.ore.HasAmount(takeOutNumMaterials))
                return takeMatFromAgentInvent = agentInventory.ore.HasAmount(takeOutNumMaterials);
            return true;
        }

        public override async Task TakeOutMaterial()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (jobSite.inventory.ingot.TryTakeOutMaterial())
                    agentInventory.ingot.TryPutInMaterial();
            }
        }

        public override async Task DeliverMaterial()
        {
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (storageSite.inventory.ingot.TryPutInMaterial())
                    agentInventory.ingot.TryTakeOutMaterial();
            }
        }
    }

    class Carpenter : JobBase
    {
        public Carpenter(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                Form1.inst.SetLabelAngle($"Task: {i}");
                await Task.Delay(jobTimeDelay);
                if (!takeMatFromAgentInvent && jobSite.inventory.ore.TryTakeOutMaterial() || takeMatFromAgentInvent && agentInventory.wood.TryTakeOutMaterial())
                    if (!jobSite.inventory.plank.TryPutInMaterial())
                        agentInventory.plank.TryPutInMaterial();
            }
        }

        public override bool HasSpaceForMaterial() => jobSite.inventory.wood.HasSpace();
        public override bool HasEnoughMaterial()
        {
            if (jobSite.inventory.wood.HasAmount(takeOutNumMaterials))
                return takeMatFromAgentInvent = (agentInventory.wood.HasAmount(takeOutNumMaterials));
            return true;
        }

        public override async Task TakeOutMaterial()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (jobSite.inventory.plank.TryTakeOutMaterial())
                    agentInventory.plank.TryPutInMaterial();
            }
        }

        public override async Task DeliverMaterial()
        {
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (storageSite.inventory.plank.TryPutInMaterial())
                    agentInventory.plank.TryTakeOutMaterial();
            }
        }
    }

    class Logger : JobBase
    {
        public Logger(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < collectNumMaterials; i++)
            {
                Form1.inst.SetLabelAngle($"Task: {i}");
                await Task.Delay(jobTimeDelay);
                if (!jobSite.inventory.ore.TryPutInMaterial())
                    agentInventory.ore.TryPutInMaterial();
            }
        }

        public override bool HasSpaceForMaterial() => jobSite.inventory.wood.HasSpace();

        public override async Task TakeOutMaterial()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (jobSite.inventory.wood.TryTakeOutMaterial())
                    agentInventory.wood.TryPutInMaterial();
            }
        }

        public override async Task DeliverMaterial()
        {
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (storageSite.inventory.wood.TryPutInMaterial())
                    agentInventory.wood.TryTakeOutMaterial();
            }
        }
    }

    class Miner : JobBase
    {
        public Miner(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob()
        {
            for (int i = 0; i < collectNumMaterials; i++)
            {
                Form1.inst.SetLabelAngle($"Task: {i}");
                await Task.Delay(jobTimeDelay);
                if (!jobSite.inventory.ore.TryPutInMaterial())
                    agentInventory.ore.TryPutInMaterial();
            }
        }

        public override bool HasSpaceForMaterial() => jobSite.inventory.ore.HasSpace();

        public override async Task TakeOutMaterial()
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (jobSite.inventory.ore.TryTakeOutMaterial())
                    agentInventory.ore.TryPutInMaterial();
            }
        }

        public override async Task DeliverMaterial()
        {
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (storageSite.inventory.ore.TryPutInMaterial())
                    agentInventory.ore.TryTakeOutMaterial();
            }
        }
    }

    class Delivery : JobBase
    {
        public Delivery(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }

        public override Task ProgressJob() => null;
        public override bool HasSpaceForMaterial() => true;

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

        public override Task TakeOutMaterial() => null;

        public override async Task TakeOutMaterial(MaterialType type)
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                switch (type)
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
            NodeType type = jobSite.nodeType;
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay);
                if (type == NodeType.CarpenterSite || type == NodeType.StorageSite)
                {
                    if (jobSite.inventory.wood.TryPutInMaterial())
                        agentInventory.wood.TryTakeOutMaterial();
                }
                else if (type == NodeType.BlacksmithSite || type == NodeType.StorageSite)
                {
                    if (jobSite.inventory.ore.TryPutInMaterial())
                        agentInventory.ore.TryTakeOutMaterial();
                }
                else if (type == NodeType.StorageSite || type == NodeType.MainSite)
                {
                    if (jobSite.inventory.ingot.TryPutInMaterial())
                        agentInventory.ingot.TryTakeOutMaterial();
                    if (jobSite.inventory.plank.TryPutInMaterial())
                        agentInventory.wood.TryTakeOutMaterial();
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

        public override bool HasSpaceForMaterial()
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