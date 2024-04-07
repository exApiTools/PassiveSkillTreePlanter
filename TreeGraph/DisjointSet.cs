using System.Collections.Generic;

namespace PassiveSkillTreePlanter.TreeGraph;

public class DisjointSet<T>
{
    private readonly Dictionary<T, int> _entityToIndexMap = [];
    private readonly List<int> _setRoots = []; // parent of i
    private readonly List<int> _setSizes = []; //number of sites in tree rooted at i

    public void AddComponent(T key)
    {
        var index = _entityToIndexMap.Count;
        _entityToIndexMap[key] = index;
        _setRoots.Add(index);
        _setSizes.Add(1);
        Count++;
    }

    public int Count { get; private set; } = 0;
    public bool IsMapped(T key) => _entityToIndexMap.ContainsKey(key);
    public IReadOnlyCollection<T> Keys => _entityToIndexMap.Keys;

    public int FindSetId(T p)
    {
        var pIndex = _entityToIndexMap[p];
        int root = pIndex;
        while (root != _setRoots[root])
            root = _setRoots[root];
        while (pIndex != root)
        {
            int newp = _setRoots[pIndex];
            _setRoots[pIndex] = root;
            pIndex = newp;
        }

        return root;
    }

    public bool AreConnected(T p, T q)
    {
        return FindSetId(p) == FindSetId(q);
    }

    public bool MergeSets(T p, T q)
    {
        int rootP = FindSetId(p);
        int rootQ = FindSetId(q);
        if (rootP == rootQ) return false;

        // make smaller root point to larger one
        if (_setSizes[rootP] < _setSizes[rootQ])
        {
            _setRoots[rootP] = rootQ;
            _setSizes[rootQ] += _setSizes[rootP];
        }
        else
        {
            _setRoots[rootQ] = rootP;
            _setSizes[rootP] += _setSizes[rootQ];
        }

        Count--;
        return true;
    }
}