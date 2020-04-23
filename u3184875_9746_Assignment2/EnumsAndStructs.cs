using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace u3184875_9746_Assignment2
{
    #region Enums
    public enum NodeType
    {
        MainSite, BlacksmithSite, CarpenterSite, StorageSite, ForestSite, MiningSite,
        ForestCenterNode, ForestNorthNode, ForestSouthNode, InterStreetNode, LuxStreetNode, NorthGateNode,
        ResidentStreetNode, SouthGateNode, WestCornerNode
    }

    public enum EdgeName
    {
        ForestOne, ForestTwo, ForestThree, ForestFour, GateOne, GateTwo, GateThree, WoodStreetOne, WoodStreetTwo,
        NorthStreetOne, NorthStreetTwo, LuxStreetOne, LuxStreetTwo, InterStreetOne, InterStreetTwo, InterStreetThree, InterStreetFour,
        ResidentStreetOne, ResidentStreetTwo, ResidentStreetThree
    }

    public enum MaterialType
    { Wood, Plank, Ore, Ingot }

    public enum Job
    { Carpenter, Logger, Blacksmith, Miner, Transporter, Constructor }
    #endregion

    #region Structs
    public struct Inventory
    {
        public MaterialBox wood;
        public MaterialBox plank;
        public MaterialBox ore;
        public MaterialBox ingot;

        public Inventory(MaterialBox newBox)
        {
            wood = newBox;
            plank = newBox;
            ore = newBox;
            ingot = newBox;
        }
    }

    public struct JobInfomation
    {
        public Job jobType;
        public int skillLevel;

        public JobInfomation(Job jobType, int skillLevel)
        {
            this.jobType = jobType;
            this.skillLevel = skillLevel;
        }
    }

    public struct CurrentJob
    {
        public Job job;
        public Site site;
        public float jobProgress;
    }

    public struct CurrentNode
    {
        public Node node;
        public Point position;

        public CurrentNode(Node node, Point position) : this()
        {
            this.node = node;
            this.position = position;
        }
    }

    public struct AgentListBox
    {
        public GroupBox agentBox;
        public PictureBox mainJob;
        public Label agentLabel;

        public GroupBox progressBox;
        public ProgressBar progressBar;
        public PictureBox progressJob;
    }

    public struct Path
    {
        public Node destination;
        public List<Node> nodes;

        public Path(Node destination, List<Node> nodes)
        {
            this.destination = destination;
            this.nodes = nodes;
        }
    }

    public struct Destination<T>
    {
        public T nodeTarget;
        public Point targetPosition;
        public float progress;

        public Destination(T nodeTarget, Point targetPosition)
        {
            this.nodeTarget = nodeTarget;
            this.targetPosition = targetPosition;
            progress = 0;
        }
    }
    #endregion
}
