#pragma once
#include "RBM.h"
namespace NeuralNetwork
{
    public ref class NeuralNetworkClass sealed
    {
    public:
		NeuralNetworkClass();
		void SetTrainingData(Windows::Foundation::Collections::IVector<Windows::Foundation::Collections::IVector<int>^> ^ data);
		void TrainRBM(int epochs, int k ,double lr);
		Windows::Foundation::Collections::IVector<double>^ Reconstruct(Windows::Foundation::Collections::IVector<int>^ input);

	private:
		int VisibleNeurons = 400;
		int HiddenLayer1 = 300;
		int OutputLayer = 10;
		RBM rbm;
		int** TrainingData;
		int trainNum;
    };
}
