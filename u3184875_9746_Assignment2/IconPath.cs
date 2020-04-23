using System.Drawing;
using System.IO;

namespace u3184875_9746_Assignment2
{
    public static class IconPath
    {
        public static Bitmap blacksmith => Properties.Resources.Icon_Blacksmith;
        public static Bitmap carpenter => Properties.Resources.Icon_Carpenter;
        public static Bitmap constructor => Properties.Resources.Icon_Constructor;
        public static Bitmap ingot => Properties.Resources.Icon_Ingot;
        public static Bitmap logger => Properties.Resources.Icon_Logger;
        public static Bitmap miner => Properties.Resources.Icon_Miner;
        public static Bitmap ore => Properties.Resources.Icon_Ore;
        public static Bitmap plank => Properties.Resources.Icon_Plank;
        public static Bitmap storage => Properties.Resources.Icon_Storage;
        public static Bitmap transporter => Properties.Resources.Icon_Transporter;
        public static Bitmap nodeIcon => Properties.Resources.Icon_Node;
        public static Bitmap agentIcon => Properties.Resources.Agent_Icon;

        public static Bitmap GetIcon(JobName job)
        {
            switch (job)
            {
                case JobName.Carpenter:
                    return carpenter;
                case JobName.Logger:
                    return logger;
                case JobName.Blacksmith:
                    return blacksmith;
                case JobName.Miner:
                    return miner;
                case JobName.Transporter:
                    return transporter;
                case JobName.Constructor:
                    return constructor;
            }
            return null;
        }

        public static Bitmap GetIcon(NodeType node)
        {
            switch (node)
            {
                case NodeType.MainSite:
                    return constructor;
                case NodeType.BlacksmithSite:
                    return blacksmith;
                case NodeType.CarpenterSite:
                    return carpenter;
                case NodeType.StorageSite:
                    return storage;
                case NodeType.ForestSite:
                    return logger;
                case NodeType.MiningSite:
                    return miner;
                default:
                    return nodeIcon;
            }
        }

        public static Bitmap GetIcon(MaterialType mat)
        {
            switch (mat)
            {
                case MaterialType.Wood:
                    return logger;
                case MaterialType.Plank:
                    return plank;
                case MaterialType.Ore:
                    return ore;
                case MaterialType.Ingot:
                    return ingot;
            }
            return null;
        }
    }
}
