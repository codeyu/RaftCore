using System.Collections.Generic;

namespace RaftCore
{
    public interface IStateMachine
    {
        void            ApplyCommands(IEnumerable<ICommand> commands);

        IList<object>   ExecuteQueries(IEnumerable<IQuery> queries);
    }
}