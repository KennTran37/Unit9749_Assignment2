using System;
using System.Threading;
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

        public MaterialBox SiteIngot => jobClass.jobSite.inventory.ingot;
        public MaterialBox SitePlank => jobClass.jobSite.inventory.plank;
        public MaterialBox SiteWood => jobClass.jobSite.inventory.wood;
        public MaterialBox SiteOre => jobClass.jobSite.inventory.ore;
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

        public abstract Task ProgressJob(Agent.UpdateProgress progress, CancellationToken ct);
        //Checks if the site has enough space for the materials that agent will put in
        public virtual Task DeliverMaterial(Agent.UpdateProgress progress, CancellationToken ct) => null;
        public virtual Task TakeOutMaterial(Agent.UpdateProgress progress, CancellationToken ct) => null;
        //used by crafters to see if both the site's and agent's inventory has enough materials to craft
        public abstract bool SpaceForAgentMaterial();
        public virtual bool HasEnoughMaterial() => true;
    }

    class BlackSmith : JobBase
    {
        public BlackSmith(Inventory agentInventory, Site jobSite) : base(agentInventory, jobSite) { }
        public override async Task ProgressJob(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, ct);
                await Task.Run(() => { progress.Invoke(i); }, ct);
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
        public override async Task ProgressJob(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, ct);
                await Task.Run(() => { progress.Invoke(i); }, ct);
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
        public override async Task ProgressJob(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < collectNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, ct);
                await Task.Run(() => { progress.Invoke(i); }, ct);
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
        public override async Task ProgressJob(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < collectNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, ct);
                await Task.Run(() => { progress.Invoke(i); }, ct);
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
        public override Task ProgressJob(Agent.UpdateProgress progress, CancellationToken ct) => null;

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

        public override async Task TakeOutMaterial(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, ct);
                await Task.Run(() => { progress.Invoke(i); }, ct);
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

        public override async Task DeliverMaterial(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                await Task.Run(() => { progress.Invoke(i); }, ct);
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
        public override async Task ProgressJob(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < takeOutNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, ct);
                await Task.Run(() => { progress.Invoke(i); }, ct);
                if (jobSite.inventory.ingot.TryTakeOutMaterial() && jobSite.inventory.plank.TryTakeOutMaterial())
                    continue;   //add one to construction progression
            }
        }

        public override bool SpaceForAgentMaterial() => jobSite.HasSpace();
        public override bool HasEnoughMaterial() => jobSite.inventory.ingot.HasAmount(takeOutNumMaterials) && jobSite.inventory.plank.HasAmount(takeOutNumMaterials);

        public override async Task TakeOutMaterial(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < TakeOutAmount; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                await Task.Run(() => { progress.Invoke(i); }, ct);
                switch (MaterialToDeliver)
                {
                    case MaterialType.Plank:
                        if (jobSite.inventory.plank.TryTakeOutMaterial())
                            agentInventory.plank.TryPutInMaterial();
                        break;
                    case MaterialType.Ingot:
                        if (jobSite.inventory.ingot.TryTakeOutMaterial())
                            agentInventory.ingot.TryPutInMaterial();
                        break;
                }
            }
        }

        public override async Task DeliverMaterial(Agent.UpdateProgress progress, CancellationToken ct)
        {
            for (int i = 0; i < putInNumMaterials; i++)
            {
                await Task.Delay(jobTimeDelay, Form1.inst.cts.Token);
                await Task.Run(() => { progress.Invoke(i); }, ct);
                switch (MaterialToDeliver)
                {
                    case MaterialType.Plank:
                        if (agentInventory.plank.TryTakeOutMaterial())
                            jobSite.inventory.plank.TryPutInMaterial();
                        break;
                    case MaterialType.Ingot:
                        if (agentInventory.ingot.TryTakeOutMaterial())
                            jobSite.inventory.ingot.TryPutInMaterial();
                        break;
                }
            }
        }
    }
}