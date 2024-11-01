﻿using CalculationTechTest.Model.DTO;
using CalculationTechTest.Services.MathService.Interface;
using CalculationTechTest.Services.Parser;
using CalculationTechTest.Utils;

namespace CalculationTechTest.Services.MathService
{
    public class CalculatorService : ICalculateServcie
    {

        public CalculatorService()
        {
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parse"></param>
        /// <param name="serializedString"></param>
        /// <returns></returns>
        public BusinessToPresentationDTO<double> Calculate(ParserHandler parser, string serializedString)
        {
            var expressionQueue = parser.ExtractExpressionQueueFromSerialized(serializedString);
            if (expressionQueue == null) // cant parse the string
            {
                return new BusinessToPresentationDTO<double>
                {
                    status = false,
                    message = "Not support format",
                };
            }

            try
            {
                List<double> result = new List<double>();
                while (expressionQueue.Count() > 0)
                {
                    var subTreeValue = CalculatorService.eval(expressionQueue);
                    result.Add(subTreeValue);
                }
                return new BusinessToPresentationDTO<double>
                {
                    status = true,
                    result = result.ToArray()
                }; 

            }
            catch (Exception ex)
            {
                return new BusinessToPresentationDTO<double>
                {
                    status = false,
                    message = ex.Message,
                };
            }
        }
        /// <summary>
        /// Functional programing:: Try to compute the double value from an expression tree
        ///     - Start of tree: [isStart: true, Operator: delegate MathOp]
        ///     - next to the last leaf of tree  is a closing tag [isStart: false, Operator: delegate MathOp]
        ///     - the tree has different layer.
        ///     - the leaf must be the a double numerical value.
        ///     - where each tree is representing a double value (after it is eval)
        ///
        /// Realistic representation:: in expression queue:
        /// 
        /// [(start) MatheOperator(+)] [double 2] [(start) MathOperator(*)] [double 3] [...] [(end ) MathOperator(*)] [...] [(end) MathOperator(+)]
        /// 
        /// 
        /// Graphical representation:: 
        ///         MathOperator:(+)                      layer: i
        ///         /               \
        ///[leaf:double]2        MathOperator:(*)         layer: i + 1
        ///                     /              \
        ///               [double]3             [(last leaf) ...]
        ///                                         
        ///                                         
        /// 
        /// where the possible objects on the queue has type: either (primitive double) or (OperatorWrapper)
        ///     - definition: OperatorWrapper {IsStart: bool, bivariateMathOperator: Func<double, double, double>}
        ///     
        /// </summary>
        /// <param name="equationResidule"> The remainning in the queue that is tobe processed</param>
        /// <param name="@mathOperator"> math operation that that in 2 input</param>
        /// <param name="value">Current value::  </param>
        /// <returns></returns>
        public static double eval(Queue<object> equationResidule)
        {
            if (equationResidule.Count == 0) return 0;

            Object nextSymbol = equationResidule.Dequeue(); 
            if (nextSymbol.IsDoubleNumber())
            {
                return nextSymbol.toDouble();
            }
            object usingOperator = ((OperationWrapper)nextSymbol).Operator;// Found a new expression tree
            double treeValue = (usingOperator.GetType() == typeof(Func<double, double>))
                ? eval(equationResidule, (Func<double, double>)usingOperator)
                : eval(equationResidule, (Func<double, double, double>)usingOperator);

            //Apply the resursively apply operator to the remain of tree (until closing tag appear)
            object followUpSymbol = null;
            equationResidule.TryPeek(out followUpSymbol);

            var nextOperator = ((OperationWrapper)followUpSymbol).Operator;
            while (followUpSymbol.IsDoubleNumber() ||
                (
                    (
                        ((OperationWrapper)followUpSymbol).IsStart || //Found a start Tag
                        !(nextOperator.IsFuncEquilvalent(usingOperator)) // found a sub tree
                    )
                )
                )
            {

                treeValue = ((Func<double, double, double>)usingOperator)(treeValue, eval(equationResidule));
                equationResidule.TryPeek(out followUpSymbol);
                nextOperator = ((OperationWrapper)followUpSymbol).Operator;
            }

            equationResidule.Dequeue(); // Remove the closing tag
            return treeValue;
        }

        /// <summary>
        /// Functional programing:: Special sub tree where the math function is a map 1:1 
        /// 
        ///     - Tree node can either be a sub tree or a leaf
        ///     - closing tag is next to the last leaf of tree. 
        ///             (*) case where the immediate node of root is subtree, then the closing tag will be 
        ///                 next to the closing tag of  that sub tree
        /// Queue representation:
        ///     [start absolute] [start +] [start negate] [-3] [end negate] [ 3] [end +] [end absolute]
        /// 
        /// Graphic representation of tree:
        /// 
        ///                  MathOperation: (absolute)
        ///                           |
        ///                           |
        ///                (last leaf(absolute))MathOperation(+)
        ///                 /                   \
        ///  [MathOperation: (negate)]          [(last leaf(+)) (double) 3]
        ///              |          
        ///              |
        ///  [(last leaf(negate)) double -3]          
        ///                 
        /// 
        /// where the possible objects on the queue has type: either (primitive double) or (OperatorWrapper)
        ///     - definition: OperatorWrapper {IsStart: bool, bivariateMathOperator: Func<double, double, double> | singleVariableOperator}
        ///     
        /// </summary>
        /// <param name="equationResidule"> the queue at this current level</param>
        /// <param name="@mathOperator"> math operation that that in 2 input</param>
        /// <param name="value">Current value::  </param>
        /// <returns></returns>
        public static double eval(Queue<object> equationResidule, Func<double, double> @singleVariableMathOperator)
        {
            if (equationResidule.Count == 0) return 0;
            double treeValue = 0;
            Object nextSymbol;
            equationResidule.TryPeek(out nextSymbol);
            // Found a leaf
            if (nextSymbol.IsDoubleNumber())
                return @singleVariableMathOperator((double)equationResidule.Dequeue());

            var nextOperator = ((OperationWrapper)nextSymbol).Operator;
            treeValue = @singleVariableMathOperator(eval(equationResidule));
            return treeValue;
        }


        /// <summary>
        /// Functional programing:: Try to compute the double value from an expression tree
        ///     - Start of tree: [isStart: true, Operator: delegate MathOp]
        ///     - next to the last leaf of tree  is a closing tag [isStart: false, Operator: delegate MathOp]
        ///     - the tree has different layer.
        ///     - the leaf must be the a double numerical value.
        ///
        /// Realistic representation:: in expression queue:
        /// 
        /// [(start) MatheOperator(+)] [double 2] [(start) MathOperator(*)] [double 3] [...] [(end ) MathOperator(*)] [...] [(end) MathOperator(+)]
        /// 
        /// 
        /// Graphical representation:: 
        ///         MathOperator:(+)                      layer: i
        ///         /               \
        ///[leaf:double]2        MathOperator:(*)         layer: i + 1
        ///                     /              \
        ///               [double]3             [(last leaf) ...]
        ///                                         
        ///                                         
        /// 
        /// where the possible objects on the queue has type: either (primitive double) or (OperatorWrapper)
        ///     - definition: OperatorWrapper {IsStart: bool, bivariateMathOperator: Func<double, double, double>}
        ///     
        /// </summary>
        /// <param name="equationResidule"> the queue at this current level</param>
        /// <param name="@mathOperator"> math operation that that in 2 input</param>
        /// <param name="value">Current value::  </param>
        /// <returns></returns>
        /// <returns></returns>
        public static double eval(Queue<object> equationResidule, Func<double, double, double> @biVariatedMathOperator)
        {
            if (equationResidule.Count == 0) return 0;
            
            double[] inputs = [(double)0, (double)0];
            for (int i = 0; i < inputs.Length; i++)
            {
                object nextSymbol = null;
                equationResidule.TryPeek(out nextSymbol);
                // if the first input is a leaf
                if ((nextSymbol.IsDoubleNumber()))
                {
                    inputs[i] = equationResidule.Dequeue()
                                                .toDouble();
                    continue;
                }
                else// evaluate the subtree
                {
                    inputs[i] = eval(equationResidule);
                }
            }
            return @biVariatedMathOperator(inputs[0], inputs[1]);
        }


    }
}
