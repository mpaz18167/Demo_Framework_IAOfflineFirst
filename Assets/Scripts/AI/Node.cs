using System.Collections.Generic;

//BASE DE LOS NODOS
public abstract class Node
{
    public enum NodeState { Running, Success, Failure}
    protected NodeState state;
    public NodeState nodeState {  get { return state; } }
    public abstract NodeState Evaluate();
}

//SELECTOR (Compuerta OR: Falla si todos sus hijos fallan)
public class Selector : Node
{
    protected List<Node> nodes = new List<Node>();
    public Selector(List<Node> nodes) {  this.nodes = nodes; }

    public override NodeState Evaluate()
    {
        foreach (Node node in nodes)
        {
            switch (node.Evaluate())
            { 
                case NodeState.Failure: continue;//intenta con el siguiemte

                case NodeState.Success:
                    state = NodeState.Success;
                    return state;

                case NodeState.Running:
                    state = NodeState.Running;
                    return state;
            }
        }
        state = NodeState.Failure; 
        return state;
    }
}



//Secuencia (Compuerta AND: Falla si uno de los hijos falla)
public class Sequence : Node
{
    protected List<Node> nodes = new List<Node>();
    public Sequence(List<Node> nodes) { this.nodes = nodes; }

    public override NodeState Evaluate()
    {
        bool anyChildIsRunning = false;
        foreach (Node node in nodes)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:
                    state= NodeState.Failure;
                    return state;

                case NodeState.Success:
                    continue;
                case NodeState.Running:
                    anyChildIsRunning= true;
                    continue;
            }
        }

        state = anyChildIsRunning ? NodeState.Running : NodeState.Success;
        return state;
    }
}