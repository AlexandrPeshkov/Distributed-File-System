using System.Collections.Generic;
using System.Linq;

namespace DFS.Node.Models
{
    /// <summary>
    /// Состояние
    /// </summary>
    public class State
    {
        /// <summary>
        /// Состояние операции
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Сообщения
        /// </summary>
        public List<string> Messages { get; set; }

        public State(bool isSuccess = true, List<string> messages = null)
        {
            IsSuccess = isSuccess;
            Messages = messages ?? new List<string>();
        }

        public static implicit operator bool(State state)
        {
            return state?.IsSuccess == true;
        }

        public static State operator +(State left, State right)
        {
            State state = null;
            if (left != null && right != null)
            {
                state = new State
                {
                    IsSuccess = left.IsSuccess && right.IsSuccess,
                    Messages = left.Messages.Concat(right.Messages).ToList()
                };
            }
            return state;
        }
    }
}