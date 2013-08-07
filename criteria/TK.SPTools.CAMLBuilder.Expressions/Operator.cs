using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace TK.SPTools.CAMLBuilder.Expressions
{
    public abstract class Operator : Expression
    {
        protected Dictionary<Expression, int> _nodes = new Dictionary<Expression, int>();
        protected abstract string OpName { get; }

        internal Operator()
        {
        }

        private int MaxDepth(Dictionary<Expression, int> expressions)
        {
            return (expressions == null || expressions.Count < 1) ? 0 : expressions.Max(p => p.Value);
        }

        internal Operator(IEnumerable<Expression> expressions)
        {
            int m = MaxDepth(_nodes);

            foreach (Expression e in expressions)
                _nodes.Add(e, m + (((e as Operator) == null) ? 0 : 1));

            //_nodes.AddRange(expressions);
        }

        public void Add(Expression expression)
        {
            int m = MaxDepth(_nodes);
            _nodes.Add(expression, m + (((expression as Operator) == null) ? 0 : 1));

            //_nodes.Add(expression);
        }

        public void AddRange(IEnumerable<Expression> expressions)
        {
            int m = MaxDepth(_nodes);

            foreach (Expression e in expressions)
                _nodes.Add(e, m + (((e as Operator) == null) ? 0 : 1));

            //_nodes.AddRange(expressions);
        }

        protected internal override string GetCAMLInternal()
        {
            if (_nodes.Count < 2)
                throw new Exception("Binary operator must have at least to operands.");

            IEnumerable<Expression> x = Flatten(new Expression[] { this });
            IEnumerable<KeyValuePair<Expression, int>> y = Flatten2(new KeyValuePair<Expression, int>[] { new KeyValuePair<Expression, int>(this,0) });

            Queue<KeyValuePair<Expression, int>> queue = new Queue<KeyValuePair<Expression, int>>(_nodes);
            return Recurse(queue);
        }

        private IEnumerable<KeyValuePair<Expression, int>> Flatten2(IEnumerable<KeyValuePair<Expression, int>> e)
        {
            return e.SelectMany(c => ((c.Key as Operator) == null) ? new KeyValuePair<Expression, int>[] { } : Flatten2(((Operator)c.Key)._nodes.ToList())).Concat(e);
        }

        private IEnumerable<Expression> Flatten(IEnumerable<Expression> e)
        {
            return e.SelectMany(c => ((c as Operator)==null) ? new Expression[] {} : Flatten(((Operator)c)._nodes.Keys.AsEnumerable())).Concat(e);
        }

        private string Recurse(Queue<KeyValuePair<Expression, int>> queue)
        {
            if (queue.Count == 2)
            {
                return string.Format(@"[{0} {1} {2}]", OpName, queue.Dequeue().Key.GetCAMLInternal(), queue.Dequeue().Key.GetCAMLInternal());
            }
            else
            {
                return string.Format(@"[{0} {1} {2}]", OpName, queue.Dequeue().Key.GetCAMLInternal(), Recurse(queue));
            }
        }

        //
        public static Or Or(params Expression[] expressions)
        {
            return new Or(expressions);
        }

        public static Or Or(IEnumerable<Expression> expressions)
        {
            return new Or(expressions);
        }

        public static And And(params Expression[] expressions)
        {
            return new And(expressions);
        }

        public static And And(IEnumerable<Expression> expressions)
        {
            return new And(expressions);
        }

        public static Not Not(params Expression[] expressions)
        {
            return new Not(expressions);
        }

        public static Not Not(IEnumerable<Expression> expressions)
        {
            return new Not(expressions);
        }
    }
}
