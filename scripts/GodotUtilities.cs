using System;
using System.Collections.Generic;
using Godot;

public static class GodotUtilities
{
    /// <summary>
    /// Finds all nodes of type T from the given parent node's children.
    /// </summary>
    public static T[] FindNodesOfType<T>(Node parentNode, bool recursive = true, int maxResults = int.MaxValue) where T : Node
	{
		List<T> nodes = new();

		for (int i = 0; i < parentNode.GetChildCount(); i++)
		{
            var childNode = parentNode.GetChild(i);

			if (childNode is T)
			{
				nodes.Add((T)childNode);

                if (nodes.Count == maxResults) break;
			}
			else
			{
				if (recursive)
				{
					T[] childResult = FindNodesOfType<T>(childNode, recursive);
					nodes.AddRange(childResult);
				}
			}
		}
		
		return nodes.ToArray();
	}

    /// <summary>
    /// Find the first node of type T from the given parent node's children. Throws if none are found.
    /// </summary>
    public static T FindNodeOfType<T>(Node parentNode, bool recursive = true) where T : Node
    {
        var results = FindNodesOfType<T>(parentNode, recursive, maxResults: 1);

        if (results.Length == 0)
        {
            throw new InvalidOperationException("Given node type was not found from the node tree.");
        }

        return results[0];
    }
}
