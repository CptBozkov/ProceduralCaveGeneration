/*
 * Project: Procedural Generation of Cave Systems
 * File: Horizons.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file defines a class for representing horizons, within the Unity environment. Horizons define the cost of
 * traveling at their height. The script includes functionality for specifying the height of each horizon and sorting them within
 * a heap based on their height. Additionally, it provides visual representation of horizons using Gizmos in the Unity Editor. 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class used to represent a horizon.
public class Horizon : MonoBehaviour, IHeapItem<Horizon>
{
    private int m_HeapIndex;

    [SerializeField][Range(0.0f, 1.0f)] private float m_Cost;

    public float Height { get => transform.position.y; }
    public float Cost { get => m_Cost; }

    public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }

    // Function used to sort inside a heap.
    public int CompareTo(object obj)
    {
        Horizon other = obj as Horizon;
        return other.Height.CompareTo(Height);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 1.0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 1.0f);
    }
}