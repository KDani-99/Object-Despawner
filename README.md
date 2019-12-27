# Object-Despawner
You can delete spawned PoolObjects (Peds,Vehicles,Blips,etc..) from the game if you define them in your class as a property or field (static,private,public,protected)

##### (Error might still occur)
## Tested with:
- Array
- ArrayList
- List
- Dictionary
- Tuple
- Queue
- Stack

#### You can even delete peds from:

```csharp
List<List<List<Dictionary<int, Ped>>>> ped = new List<List<List<Dictionary<int, Ped>>>>() { new List<List<Dictionary<int, Ped>>>() { new List<Dictionary<int, Ped>>() { new Dictionary<int, Ped>() { { 0,ped} } } } };
```

## Usage
```csharp
class MyClass
{
  public Ped ped;
  private Blip [] blips;
  public List<Ped> peds;
  private Dictionary<int,Blip> blips2;
  protected Vehicle [] vehicles;
  
  // spawn stuffs somewhere
  
}
```
and call it from somewhere else like
```csharp
  MyClass myclass = new MyClass();
  // poolobjects should be spawned
  Destructable.Destruct(myclass);
  // poolobjects should be deleted
```
