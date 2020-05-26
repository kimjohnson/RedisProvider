using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisProvider {

  public class RedisBatchProxy : IProxy {

    private readonly string _keyNameSpace;
    private List<Task> _tasks = new List<Task>();

    internal RedisBatchProxy(IBatch batch, string keyNameSpace) {
      Batch = batch;
      _keyNameSpace = keyNameSpace;
    }

    IDatabaseAsync IProxy.DB => Batch;
    string IProxy.KeyNameSpace => _keyNameSpace;

    public IBatch Batch { get; }


    /// <summary>
    /// Tasks added to the batch with AddTask.
    /// </summary>
    public IReadOnlyList<Task> Tasks { get => _tasks.AsReadOnly(); }

    /// <summary>
    /// Execute the batch, sending all queued tasks to the server.
    /// For tasks added via AddTask, the task result will be in the Tasks list.  
    /// For tasks added via WithBatch, check the result of the completed task.
    /// </summary>
    public Task Execute() {
      return Task.Run(Batch.Execute);
    }

    /// <summary>
    /// Alternative to redisObject.WithBatch(batch) to add the task to the batch.
    /// </summary>
    /// <param name="f">Delegate for any RedisObject method which returns a task.</param>
    public void AddTask(Func<Task> f) {
      var obj = f.Target;
      if (obj == null) throw new Exception("Use WithBatch() to add static methods.");

      // Replace the RedisObject with a copy which has the batch set
      var targetType = obj.GetType();
      var field = targetType.GetFields().FirstOrDefault();
      var ro = field?.GetValue(obj) as RedisObject;
      if (ro == null) throw new Exception("The object is not a RedisObject subclass.");
      var copy = ro.WithBatch(this);
      field.SetValue(obj, copy);

      _tasks.Add(f());

      // Reset the original object in the delegate now
      field.SetValue(obj, ro);
    }
  }
}
