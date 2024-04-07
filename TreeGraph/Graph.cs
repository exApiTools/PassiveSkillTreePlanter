using System.Collections.Generic;
using System.Linq;

namespace PassiveSkillTreePlanter.TreeGraph;

public class Graph
{
    private readonly Dictionary<Vertex, List<Vertex>> _adjacentVertexMap;
    private List<Edge> _edges;

    public Graph(Dictionary<Vertex, List<Vertex>> adjacentVertexMap)
    {
        _adjacentVertexMap = adjacentVertexMap;
    }

    public List<Vertex> GetAdjacent(Vertex v)
    {
        return _adjacentVertexMap[v];
    }

    public List<Edge> Edges => _edges ??= _adjacentVertexMap.SelectMany(kv => kv.Value.Select(v => new Edge(kv.Key, v))).ToList();
}

public class Edge(Vertex vertex1, Vertex vertex2)
{
    public Vertex Vertex1 { get; } = vertex1;
    public Vertex Vertex2 { get; } = vertex2;
}

public record Vertex(int Id);