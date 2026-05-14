using System.Collections.Generic;
using Godot;

public static class GodotUtilities
{
    /// <summary>
    /// Finds all nodes of type T from the given parent node's children.
    /// </summary>
    public static T[] FindNodesOfType<T>(Node parentNode, bool recursive = true) where T : Node
	{
		List<T> nodes = new();

		for (int i = 0; i < parentNode.GetChildCount(); i++)
		{
            var childNode = parentNode.GetChild(i);

			if (childNode is T)
			{
				nodes.Add((T)childNode);
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
}
