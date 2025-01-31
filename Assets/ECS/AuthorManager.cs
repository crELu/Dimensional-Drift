using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

public class AuthorManager : MonoBehaviour
{
    public List<BaseAuthor> Authors
    {
        get
        {
            var l = GetComponents<BaseAuthor>().ToList();
            if (!ValidateNoOverlappingTypes(l))
            {
                Debug.Log("Hey theres overlapping types!!!");
                l.Clear();
            }
            return l;
        }
    }

    public bool ValidateNoOverlappingTypes(List<BaseAuthor> list)
    {
        var types = list.Select(author => author.GetType()).ToList();
        var invalidPairs = new List<(Type, Type)>();

        // Compare all pairs of types for common ancestors (other than BaseAuthor)
        for (int i = 0; i < types.Count; i++)
        {
            for (int j = i + 1; j < types.Count; j++)
            {
                var commonAncestor = FindNearestCommonBaseType(types[i], types[j]);
                if (commonAncestor != null && commonAncestor != typeof(BaseAuthor))
                {
                    invalidPairs.Add((types[i], types[j]));
                }
            }
        }
        return !invalidPairs.Any();
    }

    private static Type FindNearestCommonBaseType(Type type1, Type type2)
    {
        // Traverse the type1 hierarchy
        var type1Hierarchy = new HashSet<Type>();
        while (type1 != null)
        {
            type1Hierarchy.Add(type1);
            type1 = type1.BaseType;
        }

        // Traverse the type2 hierarchy to find the first match
        while (type2 != null)
        {
            if (type1Hierarchy.Contains(type2))
                return type2;

            type2 = type2.BaseType;
        }

        return null; // No common base type found (other than object)
    }
}

public class UniversalBaker : Baker<AuthorManager>
{
    public override void Bake(AuthorManager authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        foreach (var author in authoring.Authors)
        {
            author.Bake(this, entity);
        }
    }

    public Entity ToEntity(GameObject gameObject)
    {
        return GetEntity(gameObject, TransformUsageFlags.Dynamic);
    }
    
    
}