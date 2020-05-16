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
    //nodeType is used to determine which site/node the agent is currently at
    public enum NodeType
    {
        MainSite, BlacksmithSite, CarpenterSite, StorageSite, ForestSite, MiningSite,
        ForestCenterNode, ForestNorthNode, ForestSouthNode, InterStreetNode, LuxStreetNode, NorthGateNode,
        ResidentStreetNode, SouthGateNode, WestCornerNode
    }

    //this enum is mainly used to identify an edge when the user wants to see the edge's information
    public enum EdgeName
    {
        ForestOne, ForestTwo, ForestThree, ForestFour, GateOne, GateTwo, GateThree, WoodStreetOne, WoodStreetTwo,
        NorthStreetOne, NorthStreetTwo, LuxStreetOne, LuxStreetTwo, InterStreetOne, InterStreetTwo, InterStreetThree, InterStreetFour,
        ResidentStreetOne, ResidentStreetTwo, ResidentStreetThree
    }

    //used to assign a inventories and allow the agent to identify which material they are taking out/putting in
    public enum MaterialType
    { Wood, Plank, Ore, Ingot }

    //used to identify the agent's current job and the jobs that they have
    public enum JobName
    { Carpenter, Logger, Blacksmith, Miner, Transporter, Constructor }
    #endregion

    #region Structs
    //holds information of the current node's class and it's position on the map
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

    //holds the elements which will be used to display the agent's information in the Agent List
    public struct AgentListBox
    {
        public GroupBox agentBox;
        public PictureBox mainJob;
        public Label agentLabel;

        public GroupBox progressBox;
        public ProgressBar progressBar;
        public PictureBox progressJob;
    }

    //holds the information of path that agent will take
    public struct Path
    {
        public Node start;          //the site that the agent started at
        public Node end;            //the target site the agent wanted to go to
        public List<Node> nodes;    //holds the nodes which the agent took to get to the end

        public Path(Node start, Node end, List<Node> nodes)
        {
            this.start = start;
            this.end = end;
            this.nodes = nodes;
        }
    }

    //used to mark the target site the agent wants to go to and to mark the next node/site it needs to travel to
    public struct Destination<T>    //T ise used identify the Node class or Site class
    {
        public T nodeTarget;
        public Point targetPosition;

        public Destination(T nodeTarget, Point targetPosition)
        {
            this.nodeTarget = nodeTarget;
            this.targetPosition = targetPosition;
        }
    }
    #endregion
}
