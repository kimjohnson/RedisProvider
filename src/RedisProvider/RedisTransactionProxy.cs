using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisProvider {

  public class RedisTransactionProxy : IProxy {

    private readonly string _keyNameSpace;
    private List<Task> _tasks = new List<Task>();

    internal RedisTransactionProxy(ITransaction tx, string keyNameSpace) {
      Transaction = tx;
      _keyNameSpace = keyNameSpace;
    }

    IDatabaseAsync IProxy.DB => Transaction;
    string IProxy.KeyNameSpace => _keyNameSpace;

    public ITransaction Transaction { get; }

    /// <summary>
    /// Tasks added to the transaction with AddTask.
    /// </summary>
    public IReadOnlyList<Task> Tasks { get => _tasks.AsReadOnly(); }

    public void AddCondition(Condition condition) {
      Transaction.AddCondition(condition);
    }

    /// <summary>
    /// Execute the transaction, sending all queued tasks to the server.
    /// For tasks added via AddTask, the task result will be in the Tasks list.  
    /// For tasks added via WithTx, check the result of the completed task.
    /// </summary>
    /// <returns></returns>
    public Task<bool> Execute() {
      return Transaction.ExecuteAsync();
    }

    /// <summary>
    /// Alternative to redisObject.WithTx(tx) to add the task to the transaction.
    /// </summary>
    /// <param name="f">Delegate for any RedisObject method which returns a task.</param>
    public void AddTask(Func<Task> f) {
      var obj = f.Target;
      if (obj == null) throw new Exception("Use WithTx() to add static methods.");

      // Replace the RedisObject with a copy which has the transaction set
      var targetType = obj.GetType();
      var field = targetType.GetFields().FirstOrDefault();
      var ro = field?.GetValue(obj) as RedisObject;
      if (ro == null) throw new Exception("The object is not a RedisObject subclass.");
      var copy = ro.WithTx(this);
      field.SetValue(obj, copy);

      _tasks.Add(f());

      // Reset the original object in the delegate now
      field.SetValue(obj, ro);

    }
  }
}
