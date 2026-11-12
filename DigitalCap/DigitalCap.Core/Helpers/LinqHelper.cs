using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DigitalCap.Core.Helpers
{
    public static class LinqHelper
    {
        public static string ToWhereClause(Expression expression)
        {
            var whereBuilder = new WhereBuilder();
            return whereBuilder.ToWhereClause(expression);
        }
    }

    #region WhereBuilder

    public class WhereBuilder : ExpressionVisitor
    {
        private StringBuilder? _sb;

        public int? Skip { get; private set; }

        public int? Take { get; private set; }

        public string OrderBy { get; private set; } = string.Empty;

        public string WhereClause { get; private set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public string ToWhereClause(Expression expression)
        {
            _sb = new StringBuilder();
            Visit(expression);
            WhereClause = _sb.ToString();
            return WhereClause;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                Visit(m.Arguments[0]);
                var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                Visit(lambda.Body);
                return m;
            }
            else if (m.Method.Name == "Take")
            {
                if (ParseTakeExpression(m))
                {
                    var nextExpression = m.Arguments[0];
                    return Visit(nextExpression) ?? throw new InvalidOperationException();
                }
            }
            else if (m.Method.Name == "Skip")
            {
                if (ParseSkipExpression(m))
                {
                    var nextExpression = m.Arguments[0];
                    return Visit(nextExpression) ?? throw new InvalidOperationException();
                }
            }
            else if (m.Method.Name == "OrderBy")
            {
                if (!ParseOrderByExpression(m, "ASC"))
                    throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
                var nextExpression = m.Arguments[0];
                return Visit(nextExpression) ?? throw new InvalidOperationException();
            }
            else if (m.Method.Name == "OrderByDescending")
            {
                if (!ParseOrderByExpression(m, "DESC"))
                    throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
                var nextExpression = m.Arguments[0];
                return Visit(nextExpression) ?? throw new InvalidOperationException();
            }

            throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    _sb?.Append(" NOT ");
                    Visit(u.Operand);
                    break;
                case ExpressionType.Convert:
                    Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
            }
            return u;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression b)
        {
            _sb?.Append("(");
            Visit(b.Left);

            switch (b.NodeType)
            {
                case ExpressionType.And:
                    _sb?.Append(" AND ");
                    break;

                case ExpressionType.AndAlso:
                    _sb?.Append(" AND ");
                    break;

                case ExpressionType.Or:
                    _sb?.Append(" OR ");
                    break;

                case ExpressionType.OrElse:
                    _sb?.Append(" OR ");
                    break;

                case ExpressionType.Equal:
                    _sb?.Append(IsNullConstant(b.Right) ? " IS " : " = ");
                    break;

                case ExpressionType.NotEqual:
                    _sb?.Append(IsNullConstant(b.Right) ? " IS NOT " : " <> ");
                    break;

                case ExpressionType.LessThan:
                    _sb?.Append(" < ");
                    break;

                case ExpressionType.LessThanOrEqual:
                    _sb?.Append(" <= ");
                    break;

                case ExpressionType.GreaterThan:
                    _sb?.Append(" > ");
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    _sb?.Append(" >= ");
                    break;

                default:
                    throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");

            }

            Visit(b.Right);
            _sb?.Append(")");
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression c)
        {
            var q = c.Value as IQueryable;

            if (q == null && c.Value == null)
            {
                _sb?.Append("NULL");
            }
            else if (q == null)
            {
                switch (Type.GetTypeCode(c.Value?.GetType()))
                {
                    case TypeCode.Boolean:
                        _sb?.Append((bool)c.Value! ? 1 : 0);
                        break;

                    case TypeCode.String:
                        _sb?.Append("'");
                        _sb?.Append(c.Value);
                        _sb?.Append("'");
                        break;

                    case TypeCode.DateTime:
                        _sb?.Append("'");
                        _sb?.Append(c.Value);
                        _sb?.Append("'");
                        break;

                    case TypeCode.Object:
                        throw new NotSupportedException($"The constant for '{c.Value}' is not supported");

                    default:
                        _sb?.Append(c.Value);
                        break;
                }
            }

            return c;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression m)
        {

            if (m.Expression == null) return SetNodeValue(m, m.NodeType);

            if (m.Expression.NodeType == ExpressionType.Parameter)
            {
                _sb?.Append(m.Member.Name);
                return m;
            }

            return SetNodeValue(m, m.Expression.NodeType);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        private MemberExpression SetNodeValue(MemberExpression m, ExpressionType nodeType)
        {
            if (_sb is null)
            {
                throw new InvalidOperationException("_sb cannot be null. Make sure it is initialized.");
            }
            if (nodeType == ExpressionType.MemberAccess
                || nodeType == ExpressionType.Constant)
            {
                var value = Expression.Lambda(m).Compile().DynamicInvoke();
                if (value is null) _sb?.Append($"NULL");
                else if (value is Guid) _sb?.Append($"'{value}'");
                else if (value is Enum) _sb?.Append($"{(int)value}");
                else if (value is string) _sb?.Append($"'{value}'");
                else throw new NotImplementedException($"WhereBuilder::{m.Member.Name} has not been implemented in SetMemberValue");
            }
            else
            {
                throw new NotSupportedException($"The member '{m.Member.Name}' is not supported");
            }

            return m;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        protected bool IsNullConstant(Expression exp)
        {
            var isConstant = exp.NodeType == ExpressionType.Constant;
            if (isConstant) return ((ConstantExpression)exp).Value == null;

            var val = Expression.Lambda(exp).Compile().DynamicInvoke();
            return val == null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private bool ParseOrderByExpression(MethodCallExpression expression, string order)
        {
            var unary = (UnaryExpression)expression.Arguments[1];
            var lambdaExpression = (LambdaExpression)unary.Operand;

            lambdaExpression = (LambdaExpression?)Evaluator.PartialEval(lambdaExpression);

            if (!(lambdaExpression?.Body is MemberExpression body)) return false;
            OrderBy = string.IsNullOrEmpty(OrderBy) ? $"{body.Member.Name} {order}" : $"{OrderBy}, {body.Member.Name} {order}";

            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private bool ParseTakeExpression(MethodCallExpression expression)
        {
            var sizeExpression = (ConstantExpression)expression.Arguments[1];

            if (!int.TryParse(sizeExpression.Value?.ToString(), out var size)) return false;
            Take = size;
            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private bool ParseSkipExpression(MethodCallExpression expression)
        {
            var sizeExpression = (ConstantExpression)expression.Arguments[1];

            if (!int.TryParse(sizeExpression.Value?.ToString(), out var size)) return false;
            Skip = size;
            return true;

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Evaluator
    {

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression? PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated) =>
            new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression? PartialEval(Expression expression) => PartialEval(expression, CanBeEvaluatedLocally);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static bool CanBeEvaluatedLocally(Expression expression) =>
            expression.NodeType != ExpressionType.Parameter;

        /// <summary>
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        private class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="candidates"></param>
            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                _candidates = candidates;
            }

            internal Expression? Eval(Expression? exp) => Visit(exp);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="exp"></param>
            /// <returns></returns>
            public override Expression? Visit(Expression? exp)
            {
                if (exp == null) return null;
                return _candidates.Contains(exp) ? Evaluate(exp) : base.Visit(exp);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="e"></param>
            /// <returns></returns>
            private Expression Evaluate(Expression e)
            {

                if (e.NodeType == ExpressionType.Constant) return e;

                var lambda = Expression.Lambda(e);
                var fn = lambda.Compile();

                return Expression.Constant(fn.DynamicInvoke(null), e.Type);

            }

        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        private class Nominator : ExpressionVisitor
        {
            private readonly Func<Expression, bool> _fnCanBeEvaluated;
            private HashSet<Expression>? _candidates;
            private bool _cannotBeEvaluated;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="fnCanBeEvaluated"></param>
            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                _fnCanBeEvaluated = fnCanBeEvaluated;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="expression"></param>
            /// <returns></returns>
            internal HashSet<Expression> Nominate(Expression expression)
            {
                _candidates = new HashSet<Expression>();
                Visit(expression);
                return _candidates;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="expression"></param>
            /// <returns></returns>
            public override Expression? Visit(Expression? expression)
            {
                if (expression == null) return null;
                var saveCannotBeEvaluated = _cannotBeEvaluated;

                _cannotBeEvaluated = false;

                base.Visit(expression);

                if (!_cannotBeEvaluated)
                {
                    if (_fnCanBeEvaluated(expression)) _candidates?.Add(expression);
                    else _cannotBeEvaluated = true;
                }

                _cannotBeEvaluated |= saveCannotBeEvaluated;

                return expression;

            }

        }

    }

    #endregion
}
