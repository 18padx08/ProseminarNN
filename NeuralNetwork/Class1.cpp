#include "pch.h"
#include "Class1.h"


using namespace NeuralNetwork;
using namespace Platform;

NeuralNetworkClass::NeuralNetworkClass() : rbm(28*28,500,NULL,NULL,NULL)
{
	/*TrainingData = new int*[trainNum]();
	for (int i = 0; i < trainNum; i++) {
		TrainingData[i] = new int[28*28]();
	}*/
}

void NeuralNetwork::NeuralNetworkClass::SetTrainingData(Windows::Foundation::Collections::IVector<Windows::Foundation::Collections::IVector<int>^> ^data)
{
	//allocate arrays according to test data
	trainNum = data->Size;

	TrainingData = new int*[trainNum]();
	for (int i = 0; i < trainNum; i++) {
	TrainingData[i] = new int[28*28]();
	}

	for (int o = 0; o < trainNum; o++) {
		auto traind = data->GetAt(o);
		for (int i = 0; i < 28*28; i++) {
			
			TrainingData[o][i] = traind->GetAt(i);
		}
	}
}

void NeuralNetwork::NeuralNetworkClass::TrainRBM(int epochs, int k, double lr)
{
	for (int epochNum = 0; epochNum < epochs; epochNum++) {
		for (int trainSamp = 0; trainSamp < trainNum; trainSamp++) {
			rbm.contrastive_divergence(TrainingData[trainSamp], lr, k, trainNum);
		}
	}
}

Windows::Foundation::Collections::IVector<double>^ NeuralNetwork::NeuralNetworkClass::Reconstruct(Windows::Foundation::Collections::IVector<int>^ input)
{
	int arrayinput[28 * 28];
	double arrayoutput[28 * 28];
	for (int i = 0; i < 28 * 28;i++) {
		arrayinput[i] = input->GetAt(i);
	}
	rbm.reconstruct(arrayinput, arrayoutput);
	return ref new Platform::Collections::Vector<double>(arrayoutput);
}
