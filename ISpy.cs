using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// ISpy lets you spy on your Unity objects. This allows us to do things
/// like synchronizing the state of objects in a networked environment, logging
/// how parameters are changing over time (without throwing around Debug.Logs),
/// and just generally move towards an event-driven style of programming.
/// </summary>
namespace ISpy
{
    /// <summary>
    /// Struct to describe a change in object state.
    /// </summary>
    /// <remarks>
    /// For purposes of logging and synchronizing state between multiple clients.
    /// </remarks>
    public readonly struct StateChange
    {
        public object obj { get; }
        public object property { get; }
        public object value { get; }

        public StateChange(object o, object p, object v)
        {
            obj = o;
            property = p;
            value = v;
        }
    }

    /// <summary>
    /// Class to inherit from (instead of MonoBehaviour) in order to
    /// attach a Spy and track parameter changes.
    /// </summary>
    /// <remarks>
    /// Spyable initializes your spy by default in Awake, so you may
    /// override the Awake function if you want to disable the spy.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyComponent : Spyable
    /// {
    ///     private float number;
    ///     
    ///     public float MyNumber
    ///     {
    ///         get
    ///         {
    ///             return number;
    ///         }
    ///         set
    ///         {
    ///             number = value;
    ///             Leak();
    ///         }
    ///     }
    /// 
    ///     override private void Awake()
    ///     {
    ///         // Comment out line below to disable spy
    ///         base.Awake();
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Spyable : MonoBehaviour, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static List<Spy> spies = new List<Spy>();

        /// <summary>
        /// Awake function that can be overwritten by inheriting components.
        /// </summary>
        /// <remarks>
        /// If overwritten, be sure to call base.Awake() in order to still
        /// add the spy.
        /// </remarks>
        protected virtual void Awake()
        {
            spies.Add(new Spy(this));
        }

        /// <summary>
        /// When called from a property setter, this method will leak the
        /// calling class, the property being set, and the value set back
        /// to all spies by invoking its PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">DO NOT SET. The runtime will fill this with the name of the property being set.</param>        
        public void Leak([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Class for tracking changes in a parameter.
    /// </summary>
    public class Spy
    {
        public Logging.PropertyLog log;

        private Spyable obj;

        #region CONSTRUCTORS
        public Spy(Spyable o)
        {
            log = new Logging.PropertyLog();
            obj = o;
            obj.PropertyChanged += log.Append;
        }

        public Spy(Spyable o, PropertyChangedEventHandler cb)
        {
            log = new Logging.PropertyLog();
            obj = o;
            obj.PropertyChanged += cb;
        }
        #endregion

        #region ADDERS
        /// <summary>
        /// Method to add a new callback function to an existing spy.
        /// </summary>
        /// <param name="cb">Callback function that reacts to PropertyChanged events to add.</param>
        public void Add(PropertyChangedEventHandler cb)
        {
            obj.PropertyChanged += cb;
        }

        /// <summary>
        /// Method to add a new callback function to an existing spy.
        /// </summary>
        /// <param name="spy">Spy to add callback to.</param>
        /// <param name="function">Callback function to add to spy.</param>
        /// <returns>Spy with added callback.</returns>
        public static Spy operator+ (Spy spy, PropertyChangedEventHandler function)
        {
            spy.obj.PropertyChanged += function;
            return spy;
        }
        #endregion

        #region REMOVERS
        /// <summary>
        /// Method to remove an existing callback function from an existing spy.
        /// </summary>
        /// <param name="cb">Callback function that reacts to PropertyChanged events to remove.</param>
        public void Remove(PropertyChangedEventHandler cb)
        {
            obj.PropertyChanged -= cb;
        }

        /// <summary>
        /// Method to remove an existing callback function from an existing spy.
        /// </summary>
        /// <param name="spy">Spy to remove callback from.</param>
        /// <param name="function">Callback function to remove from spy.</param>
        /// <returns>Spy with removed callback.</returns>
        public static Spy operator- (Spy spy, PropertyChangedEventHandler function)
        {
            spy.obj.PropertyChanged -= function;
            return spy;
        }
        #endregion

    }
    
    /// <summary>
    /// Class for tracking GameObject positions.
    /// </summary>
    /// <remarks>
    /// This class maintains a set of tracked transforms, with methods
    /// to add and remove them. Used in the TrackedObject and 
    /// OmniscientTracker components.
    /// </remarks>
    public class Tracker
    {
        public readonly HashSet<Transform> tracked;

        #region CONSTRUCTORS
        public Tracker(GameObject obj)
        {
            tracked = new HashSet<Transform>();
            tracked.Add(obj.transform);
        }

        public Tracker(GameObject[] objs)
        {
            tracked = new HashSet<Transform>();
            foreach (GameObject obj in objs)
            {
                tracked.Add(obj.transform);
            }
        }

        public Tracker(List<GameObject> objs)
        {
            tracked = new HashSet<Transform>();
            foreach (GameObject obj in objs)
            {
                tracked.Add(obj.transform);
            }
        }

        public Tracker(IEnumerable<GameObject> objs)
        {
            tracked = new HashSet<Transform>();
            foreach (GameObject obj in objs)
            {
                tracked.Add(obj.transform);
            }
        }
        #endregion

        #region ADDERS
        public void Add(GameObject obj)
        {
            tracked.Add(obj.transform);
        }

        public void Add(GameObject[] objs)
        {
            foreach (GameObject obj in objs)
            {
                tracked.Add(obj.transform);
            }
        }

        public void Add(List<GameObject> objs)
        {
            foreach (GameObject obj in objs)
            {
                tracked.Add(obj.transform);
            }
        }

        public void Add(IEnumerable<GameObject> objs)
        {
            foreach (GameObject obj in objs)
            {
                tracked.Add(obj.transform);
            }
        }
        #endregion

        #region REMOVERS
        public bool TryRemove(GameObject obj)
        {
            return tracked.Remove(obj.transform);
        }

        public int TryRemove(List<GameObject> objs)
        {
            return tracked.RemoveWhere(t => objs.Contains(t.gameObject));
        }
        #endregion

    }

    /// <summary>
    /// Component for tracking GameObject positions, specified to GameObjects
    /// with a component of type T.
    /// </summary>
    /// <remarks>
    /// This component maintains a set of tracked transforms, with methods
    /// to add and remove them.
    /// </remarks>
    public class Tracker<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        public readonly HashSet<Transform> tracked;

        public Tracker()
        {
            tracked = new HashSet<Transform>();
            T[] components = FindObjectsOfType<T>();
            foreach (T component in components)
            {
                tracked.Add(component.transform);
            }
        }

        private void Update()
        {
            T[] components = FindObjectsOfType<T>();
            foreach (T component in components)
            {
                tracked.Add(component.transform);
            }
        }

                private void LateUpdate()
        {
            foreach (Transform t in tracker.tracked)
            {
                if (t.hasChanged)
                {
                    StateChange change = new StateChange(t.gameObject, typeof(Transform), t);
                    log.Append(Time.time, change);
                }
            }
        }
    }

    /// <summary>
    /// Component for tracking and logging all movements of a GameObject.
    /// </summary>
    public class TrackedObject : MonoBehaviour
    {
        private Tracker tracker;
        private Logging.StateLog log;

        public event Logging.AppendHandler<float, StateChange> OnAppend
        {
            add
            {
                log.OnAppend += value;
            }
            remove
            {
                log.OnAppend -= value;
            }
        }

        private void Awake()
        {
            tracker = new Tracker(gameObject);
            log = new Logging.StateLog();
        }

        private void LateUpdate()
        {
            foreach (Transform t in tracker.tracked)
            {
                if (t.hasChanged)
                {
                    StateChange change = new StateChange(t.gameObject, typeof(Transform), t);
                    log.Append(Time.time, change);
                }
            }
        }
    }

    /// <summary>
    /// Component for tracking and logging all movements within a scene.
    /// </summary>
    public class OmniscientTracker : MonoBehaviour
    {
        private Tracker tracker;
        private Logging.StateLog log;

        /// <summary>
        /// Function for callbacks to execute whenever a tracked object moves.
        /// </summary>
        /// <remarks>
        /// In order to have your callback called, add it to OnAppend
        /// (OnAppend += YourCallback). You may also stop calling it
        /// by subtracting it from OnAppend (OnAppend -= YourCallback).
        /// </remarks>
        public event Logging.AppendHandler<float, StateChange> OnAppend
        {
            add
            {
                log.OnAppend += value;
            }
            remove
            {
                log.OnAppend -= value;
            }
        }

        private void Awake()
        {
            log = new Logging.StateLog();
            GameObject[] objects = FindObjectsOfType<GameObject>();
            tracker = new Tracker(objects);
        }

        private void Update()
        {
            tracker.Add(FindObjectsOfType<GameObject>());
        }

        private void LateUpdate()
        {
            foreach (Transform t in tracker.tracked)
            {
                if (t.hasChanged)
                {
                    StateChange change = new StateChange(t.gameObject, typeof(Transform), t);
                    log.Append(Time.time, change);
                    t.hasChanged = false;
                }
            }
        }
    }

    /// <summary>
    /// Component for tracking and logging all movements within a scene
    /// specified to GameObjects with a component of type T.
    /// </summary>
    [Obsolete("OmniscientTracker for specified MonoBehaviours is currently non-functional" +
                " because Unity does not allow for generic MonoBehaviours.")]
    public class OmniscientTracker<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private Tracker<T> tracker;
        private Logging.StateLog log;

        /// <summary>
        /// Function for callbacks to execute whenever a tracked object moves.
        /// </summary>
        /// <remarks>
        /// In order to have your callback called, add it to OnAppend
        /// (OnAppend += YourCallback). You may also stop calling it
        /// by subtracting it from OnAppend (OnAppend -= YourCallback).
        /// </remarks>
        public event Logging.AppendHandler<float, StateChange> OnAppend
        {
            add
            {
                log.OnAppend += value;
            }
            remove
            {
                log.OnAppend -= value;
            }
        }

        private void Awake()
        {
            throw new NotImplementedException("OmniscientTracker for specified MonoBehaviours is currently non-functional" +
                " because Unity does not allow for generic MonoBehaviours.");
            log = new Logging.StateLog();
            tracker = new Tracker<T>();
        }

        private void LateUpdate()
        {
            foreach (Transform t in tracker.tracked)
            {
                if (t.hasChanged)
                {
                    StateChange change = new StateChange(t.gameObject, typeof(Transform), t);
                    log.Append(Time.time, change);
                    t.hasChanged = false;
                }
            }
        }
    }

    /// <summary>
    /// Classes for logging the things that your spies see.
    /// </summary>
    namespace Logging
    {
        /// <summary>
        /// A subclass of event arguments, for custom behavior whenever a new log entry is made.
        /// </summary>
        /// <typeparam name="T">Type of the log timestamp</typeparam>
        /// <typeparam name="D">Type of the log data</typeparam>
        public class LogArgs<T, D> : EventArgs
        {
            public T timestamp { get; set; }
            public D data { get; set; }

            public LogArgs(T t, D d)
            {
                timestamp = t;
                data = d;
            }
        }

        /// <summary>
        /// The form that any callback function you want to add to OnAppend must take.
        /// </summary>
        /// <typeparam name="T">Type of the log timestamp</typeparam>
        /// <typeparam name="D">Type of the log data</typeparam>
        /// <param name="args">Log entry passed to the callback</param>
        public delegate void AppendHandler<T, D>(LogArgs<T, D> args);

        /// <summary>
        /// Append-only log interface, with an IComparable type T for its timestamps and type D for its data.
        /// </summary>
        /// <typeparam name="T">Type of log timestamp</typeparam>
        /// <typeparam name="D">Type of log entry</typeparam>
        public interface Log<T, D>
            where T : IComparable
        {
            event AppendHandler<T, D> OnAppend; 
            void Append(T timestamp, D data);
        }

        /// <summary>
        /// Base class for producing a log of records, implemented
        /// as an ordered dictionary.
        /// </summary>
        /// <remarks>
        /// This class is not thread safe. State changes should all be
        /// committed/read on the same thread, ideally. The first type,
        /// T, is the type of the log keys (for example, time or some other
        /// ordered key).
        /// </remarks>
        public class DictionaryLog<T, D> : Log<T, D>
            where T : IComparable
        {
            protected SortedDictionary<object, Collection<D>> records;

            public event AppendHandler<T, D> OnAppend;

            public DictionaryLog()
            {
                records = new SortedDictionary<object, Collection<D>>();
            }

            public virtual void Append(T timestamp, D state)
            {
                if (records.ContainsKey(timestamp))
                {
                    records[timestamp].Add(state);
                } else
                {
                    records[timestamp] = new Collection<D>();
                    records[timestamp].Add(state);
                }
                OnAppend?.Invoke(new LogArgs<T, D>(timestamp, state));
            }
        }

        /// <summary>
        /// Class for producing an ordered log of StateChange records.
        /// </summary>
        /// <remarks>
        /// This class is not thread safe. State changes should all be
        /// committed/read on the same thread, ideally.
        /// </remarks>
        public class StateLog : DictionaryLog<float, StateChange>
        {
            public override void Append(float timestamp, StateChange state)
            {
                base.Append(timestamp, state);
            }
        }

        /// <summary>
        /// A default property change logging class.
        /// </summary>
        /// <remarks>
        /// This class is not thread safe. State changes should all be
        /// committed/read on the same thread, ideally.
        /// </remarks>
        public class PropertyLog : StateLog
        {
            /// <summary>
            /// A default property logging function.
            /// </summary>
            /// <remarks>
            /// Callbacks such as this one may be added or removed from the spy
            /// with the += and -= syntax (or the .Add() and .Remove() methods).
            /// </remarks>
            /// <param name="sender">Object being spied on.</param>
            /// <param name="args">Property changed object, passed on calling Leak().</param>
            public void Append(object sender, PropertyChangedEventArgs args)
            {
                Type type = sender.GetType();
                PropertyInfo property = type.GetProperty(args.PropertyName);
                object value = property.GetValue(sender);
                StateChange change = new StateChange(sender, property, value);
                base.Append(Time.time, change);
            }
        }
    }

}