namespace u3184875_9746_Assignment2
{
    public class Edge
    {
        public EdgeName name;
        public int cost = 1;
        public Node pointOne;
        public Node pointTwo;

        public Edge(EdgeName name, int cost, Node pointOne, Node pointTwo)
        {
            this.name = name;
            this.cost = cost;
            this.pointOne = pointOne;
            this.pointTwo = pointTwo;
        }

        public Node GetOtherPoint(Node currentNode)
        {
            if (pointOne == currentNode)
                return pointTwo;
            if (pointTwo == currentNode)
                return pointOne;
            return null;
        }

        public bool HasNode(Node node) => pointOne == node || pointTwo == node;
    }
}
