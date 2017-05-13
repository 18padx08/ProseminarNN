#pragma once
#include <mutex>

class RBM {

public:
	
	int n_visible;
	int n_hidden;
	double **W;
	double *hbias;
	double *vbias;
	RBM( int, int, double**, double*, double*);
	~RBM();
	void contrastive_divergence(int*, double, int, int);
	void sample_h_given_v(int*, double*, int*);
	void sample_v_given_h(int*, double*, int*);
	double propup(int*, double*, double);
	double propdown(int*, int, double);
	void gibbs_hvh(int*, double*, int*, double*, int*);
	void reconstruct(int*, double*);
private:
	std::mutex mtx;
};

