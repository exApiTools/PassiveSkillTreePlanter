using System.Collections.Generic;
using System.Linq;

namespace PassiveSkillTreePlanter.TreeGraph;

public class GraphOptimizer
{
    public static List<Vertex> ReduceGraph(Graph g, List<Vertex> requiredVertices)
    {
        var remainingSlack = new Dictionary<Vertex, float>();
        var addedVertices = new List<Vertex>();
        var componentSet = new DisjointSet<Vertex>();

        void PickVertex(Vertex vertex)
        {
            componentSet.AddComponent(vertex);
            foreach (var adjacent in g.GetAdjacent(vertex))
            {
                if (componentSet.IsMapped(adjacent))
                {
                    componentSet.MergeSets(adjacent, vertex);
                }
                else
                {
                    remainingSlack.TryAdd(adjacent, 1);
                }
            }
        }

        foreach (var requiredVertex in requiredVertices)
        {
            PickVertex(requiredVertex);
        }

        while (remainingSlack.Any() && componentSet.Count > 1)
        {
            var stepSize = remainingSlack
                .Select(x => (x, x.Value / g.GetAdjacent(x.Key).Where(componentSet.IsMapped).Select(componentSet.FindSetId).Distinct().DefaultIfEmpty().Count()))
                .ToList();
            var minStepSize = stepSize.Min(x => x.Item2);
            foreach (var ((vertex, remainingVertexSlack), vertexStepSize) in stepSize)
            {
                if (vertexStepSize <= minStepSize)
                {
                    remainingSlack.Remove(vertex);
                    addedVertices.Add(vertex);
                    PickVertex(vertex);
                }
                else
                {
                    remainingSlack[vertex] = remainingVertexSlack * (1 - minStepSize / vertexStepSize);
                }
            }
        }

        addedVertices.Reverse();
        var result = componentSet.Keys.ToHashSet();
        foreach (var addedVertex in addedVertices)
        {
            if (result.Remove(addedVertex) && 
                !EnsureConnected(g, result, requiredVertices))
            {
                result.Add(addedVertex);
            }
        }

        return result.ToList();
    }

    private static bool EnsureConnected(Graph g, HashSet<Vertex> availableVertices, List<Vertex> verticesToCheck)
    {
        if (verticesToCheck.Count <= 1) return true;
        var vertexQueue = new Queue<Vertex>();
        vertexQueue.Enqueue(verticesToCheck[0]);
        availableVertices = availableVertices.ToHashSet();
        availableVertices.Remove(verticesToCheck[0]);
        var unconnectedVertices = verticesToCheck.Skip(1).ToHashSet();
        while (vertexQueue.TryDequeue(out var vertex))
        {
            if (unconnectedVertices.Remove(vertex) && unconnectedVertices.Count == 0)
            {
                return true;
            }

            foreach (var adjacent in g.GetAdjacent(vertex).Where(availableVertices.Remove))
            {
                vertexQueue.Enqueue(adjacent);
            }
        }

        return false;
    }
}