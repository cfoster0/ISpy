# ISpy
C# tools for tracking changes to Unity objects, to make synchronization and logging easier.

## Spies

### ```Spyable```

Class to inherit from (instead of ```MonoBehaviour```) in order to attach a [```Spy```](#spy) and track parameter changes. ```Spyable``` initializes your ```Spy``` by default in ```Awake```, so you may override the ```Awake``` function if you want to disable the ```Spy```, or if you want to initialize a custom spy.

In a ```Spyable``` component, adding a call to ```Leak()``` in a setter function will alert all of the component's spies whenever the value is set.

**Example.** The following is a ```Spyable``` with a property ```number``` that is leaked. In this case, the leaked information will consist of a [```StateChange```](#statechange) with ```MyComponent``` as the ```.obj``` field, ```MyNumber``` as the ```.property``` field, and the new value of ```number``` as the ```.value``` field.

```c#
public class MyComponent : Spyable
{
	private float number;

    public float MyNumber
	{
		get
		{
			return number;
		}
		set
		{
			number = value;
            Leak();
		}
	}

	override private void Awake()
	{
        // Comment out line below to disable spy
        base.Awake();
	}
}
```

### ```Spy```

Class for tracking changes in a parameter. The ```Spy``` class also contains methods for adding and removing callbacks that will be executed whenever a [```PropertyChanged```](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged.propertychanged?view=netframework-4.8) event is invoked on its associated [```Spyable```](#spyable). By default, it executes the ```Append``` method on its associated ```PropertyLog``` whenever ```PropertyChanged``` events are invoked. Callbacks to be executed for new ```PropertyChangedEventArgs``` records can be attached to a ```Spy ``` with either ```+=``` syntax or the ```.Add(...)``` method of the ```Spy```, and detached from it with either ```-=``` syntax or the ```.Remove(...)``` method of the ```Spy```.

### ```StateChange```

Struct to describe a change in object state. For purposes of logging and synchronizing state between multiple clients.

| Field        | Type         | Example                    | Remarks                                                      |
| ------------ | ------------ | -------------------------- | ------------------------------------------------------------ |
| **obj**      | ```object``` | ```MyComponent``` instance | Object whose state is being set. Typically the ```MonoBehaviour``` whose property was changed. |
| **property** | ```object``` | ```"position"```           | Property whose value changed.                                |
| **value**    | ```object``` | ```Vector3(0, 0, 0)```     | New value of **property**.                                   |

```StateChange```s are passed by [```Spyable```](#spyable) components whenever the value of one of their spied-on properties is set.

## Trackers

Trackers are components that track ```GameObject``` movements. At the moment, their interface is different from the ```Spyable``` interface, since we only need to track movements on a frame-by-frame basis, rather than in real time.

### ```Tracker<T>```

Component for tracking ```GameObject``` positions, specified to ```GameObject```s with a component of type ```T```.

**Example**. The following example creates a tracker for a custom component and adds a callback  to the tracker. This callback will execute whenever the transform of an object with the component changes.

```c#
Tracker<MyComponent> tracker = new Tracker<MyComponent>();
tracker.OnAppend += AppendCallback;

private void AppendCallback(float time, StateChange change) {
    GameObject g = (GameObject)change.obj;
    Transform = change.property;
    Transform t = change.value;
    ...
};
```

### ```OmniscientTracker```

Component for tracking and logging all movements (```Transform``` changes) within a scene.

**Example**. The following example creates an omniscient tracker and adds a callback to it. This callback will execute whenever the transform of any active ```GameObject``` changes.

```c#
OmniscientTracker tracker = new OmniscientTracker();
tracker.OnAppend += AppendCallback;

private void AppendCallback(float time, StateChange change) {
    GameObject g = (GameObject)change.obj;
    Transform = change.property;
    Transform t = change.value;
    ...
};
```

### ```TrackedObject```

Component for tracking and logging all movements (```Transform``` changes) of a ```GameObject```.

**Example**. The following example creates an omniscient tracker and adds a callback to it. This callback will execute whenever the transform of any active ```GameObject``` changes.

```c#
TrackedObject tracker = new TrackedObject(gameObject);
tracker.OnAppend += AppendCallback;

private void AppendCallback(float time, StateChange change) {
    GameObject g = (GameObject)change.obj;
    Transform = change.property;
    Transform t = change.value;
    ...
};
```

## Logs

### ```Log<T,D>```

Append-only log interface, with an [```IComparable```](https://docs.microsoft.com/en-us/dotnet/api/system.icomparable?view=netframework-4.8) type ```T``` for its timestamps and type ```D``` for its data. Must implement the ```Append(T timestamp, D data)``` method and have an ```AppendHandler<T, D>``` event. 

### ```StateLog```

Class for producing an ordered log of [```StateChange```](#statechange) records. Implements the ```Log<T,D>``` interface. Callbacks to be executed for new ```StateChange``` records can be attached to a ```StateLog``` with either ```+=``` syntax or the ```.Add(...)``` method, and detached from it with either ```-=``` syntax or the ```.Remove(...)``` method.

**Note**: This class is not thread safe. State changes should all be committed/read on the same thread, ideally.

### ```PropertyLog```

A default property change logging class, used by ```Spy``` objects. Implements the ```Log<T,D>``` interface. 

**Note**: This class is not thread safe. State changes should all be committed/read on the same thread, ideally.