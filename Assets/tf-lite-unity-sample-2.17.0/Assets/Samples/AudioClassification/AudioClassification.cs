using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace TensorFlowLite
{
    public sealed class AudioClassification : IDisposable
    {
        public readonly struct Label : IComparable<Label>
        {
            public readonly int id;
            public readonly float score;
            public Label(int id, float score)
            {
                this.id = id;
                this.score = score;
            }
            public int CompareTo(Label other)
            {
                return other.score.CompareTo(score);
            }
        }

        private readonly Interpreter interpreter;
        private readonly float[] input;
        private readonly NativeArray<float> output;
        private NativeArray<Label> labels;

        public float[] Input => input;

        public AudioClassification(byte[] modelData)
        {
            var interpreterOptions = new InterpreterOptions();
            interpreterOptions.AutoAddDelegate(TfLiteDelegateType.XNNPACK, typeof(float));
            interpreter = new Interpreter(modelData, interpreterOptions);
            interpreter.LogIOInfo();

            // Allocate IO buffers
            int inputLength = interpreter.GetInputTensorInfo(0).GetElementCount();
            input = new float[inputLength];
            int outputLength = interpreter.GetOutputTensorInfo(0).GetElementCount();
            output = new NativeArray<float>(outputLength, Allocator.Persistent);
            labels = new NativeArray<Label>(output.Length, Allocator.Persistent);
            interpreter.AllocateTensors();
        }

        public void Dispose()
        {
            interpreter?.Dispose();
            labels.Dispose();
            output.Dispose();
        }

        public void Run()
        {
            interpreter.SetInputTensorData(0, input);
            interpreter.Invoke();
            interpreter.GetOutputTensorData(0, output.AsSpan());

            var job = new OutPutToLabelJob()
            {
                input = output,
                output = labels,
            };
            job.Schedule().Complete();
        }

        public NativeSlice<Label> GetTopLabels(int topK)
        {
            return labels.Slice(0, Math.Min(topK, labels.Length));
        }

        //[BurstCompile]
        internal struct OutPutToLabelJob : IJob
        {
            [ReadOnly]
            public NativeSlice<float> input;
            [WriteOnly]
            public NativeSlice<Label> output;

            public void Execute()
            {
                // First, create the labels in the output array
                for (int i = 0; i < input.Length; i++)
                {
                    output[i] = new Label(i, input[i]);
                }

                // Bubble sort implementation that works with NativeArray
                // We use bubble sort for simplicity, but for production you might want
                // to implement a more efficient algorithm like QuickSort
                for (int i = 0; i < input.Length - 1; i++)
                {
                    for (int j = 0; j < input.Length - i - 1; j++)
                    {
                        if (output[j].CompareTo(output[j + 1]) > 0)
                        {
                            // Swap
                            Label temp = output[j];
                            output[j] = output[j + 1];
                            output[j + 1] = temp;
                        }
                    }
                }
            }
        }
    }
}