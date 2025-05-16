using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GeoJSONTools.Editor
{
    /// <summary>
    /// Utility class for running coroutines in the Unity Editor.
    /// Allows for non-blocking operations in editor windows.
    /// </summary>
    public static class EditorCoroutineUtility
    {
        private class EditorCoroutine
        {
            public IEnumerator Routine { get; private set; }
            public Action<bool> OnComplete { get; private set; }
            public bool IsComplete { get; private set; }
            
            private Stack<IEnumerator> routineStack = new Stack<IEnumerator>();
            
            public EditorCoroutine(IEnumerator routine, Action<bool> onComplete = null)
            {
                Routine = routine;
                OnComplete = onComplete;
                IsComplete = false;
                routineStack.Push(routine);
            }
            
            public void Update()
            {
                if (routineStack.Count == 0)
                {
                    IsComplete = true;
                    OnComplete?.Invoke(true);
                    return;
                }
                
                IEnumerator currentRoutine = routineStack.Peek();
                bool moveNext = currentRoutine.MoveNext();
                
                if (!moveNext)
                {
                    routineStack.Pop();
                    Update();
                    return;
                }
                
                object current = currentRoutine.Current;
                
                if (current == null)
                {
                    return;
                }
                else if (current is WaitForSeconds)
                {
                    float seconds = (float)current.GetType().GetField("m_Seconds").GetValue(current);
                    double targetTime = EditorApplication.timeSinceStartup + seconds;
                    EditorApplication.delayCall += () => DelayedResume(targetTime);
                }
                else if (current is WaitForSecondsRealtime)
                {
                    float seconds = (float)current.GetType().GetField("m_Seconds").GetValue(current);
                    double targetTime = EditorApplication.timeSinceStartup + seconds;
                    EditorApplication.delayCall += () => DelayedResume(targetTime);
                }
                else if (current is IEnumerator)
                {
                    routineStack.Push(current as IEnumerator);
                }
                else if (current is CustomYieldInstruction)
                {
                    // Handle custom yield instructions by checking keepWaiting
                    var customYield = current as CustomYieldInstruction;
                    if (customYield.keepWaiting)
                    {
                        EditorApplication.delayCall += () => Update();
                    }
                }
            }
            
            private void DelayedResume(double targetTime)
            {
                if (EditorApplication.timeSinceStartup >= targetTime)
                {
                    Update();
                }
                else
                {
                    EditorApplication.delayCall += () => DelayedResume(targetTime);
                }
            }
            
            public void Stop()
            {
                routineStack.Clear();
                IsComplete = true;
                OnComplete?.Invoke(false);
            }
        }
        
        private static List<EditorCoroutine> activeCoroutines = new List<EditorCoroutine>();
        private static bool isUpdating = false;
        
        static EditorCoroutineUtility()
        {
            EditorApplication.update += UpdateCoroutines;
        }
        
        /// <summary>
        /// Starts a coroutine in the editor.
        /// </summary>
        /// <param name="routine">The coroutine to start.</param>
        /// <param name="onComplete">Optional callback when the coroutine completes.</param>
        /// <returns>A handle to the coroutine that can be used to stop it.</returns>
        public static object StartCoroutine(IEnumerator routine, Action<bool> onComplete = null)
        {
            if (routine == null)
                return null;
                
            EditorCoroutine coroutine = new EditorCoroutine(routine, onComplete);
            activeCoroutines.Add(coroutine);
            
            if (!isUpdating)
            {
                coroutine.Update();
            }
            
            return coroutine;
        }
        
        /// <summary>
        /// Stops a running coroutine.
        /// </summary>
        /// <param name="coroutineHandle">The handle returned by StartCoroutine.</param>
        public static void StopCoroutine(object coroutineHandle)
        {
            if (coroutineHandle == null || !(coroutineHandle is EditorCoroutine))
                return;
                
            EditorCoroutine coroutine = coroutineHandle as EditorCoroutine;
            if (activeCoroutines.Contains(coroutine))
            {
                coroutine.Stop();
                activeCoroutines.Remove(coroutine);
            }
        }
        
        /// <summary>
        /// Stops all running coroutines.
        /// </summary>
        public static void StopAllCoroutines()
        {
            foreach (var coroutine in activeCoroutines)
            {
                coroutine.Stop();
            }
            
            activeCoroutines.Clear();
        }
        
        private static void UpdateCoroutines()
        {
            if (activeCoroutines.Count == 0)
                return;
                
            isUpdating = true;
            
            for (int i = activeCoroutines.Count - 1; i >= 0; i--)
            {
                if (i >= activeCoroutines.Count)
                    continue;
                    
                EditorCoroutine coroutine = activeCoroutines[i];
                if (coroutine.IsComplete)
                {
                    activeCoroutines.RemoveAt(i);
                }
                else
                {
                    coroutine.Update();
                }
            }
            
            isUpdating = false;
        }
    }
}
