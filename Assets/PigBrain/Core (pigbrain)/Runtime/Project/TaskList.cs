using System.Collections.Generic;
using pigbrain.core.Project;
using UnityEngine;

namespace pigbrain.core.Project
{
    [System.Serializable]
    public class TaskList : ScriptableObject
    {
        public List<Task> tasks = new();

        [System.Serializable]
        public class Task
        {
            public string name;
            public float duration;
            public Status status = Status.ToDo;

            public enum Status { Doing, ToDo, Done, Hold }
        }
    }
}