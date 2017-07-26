#pragma once
#include <algorithm> // For for_each, transform, adjacent_find, count_if, accumulate, copy, max_element
#include <bitset>
#include <cassert>
#include <cctype> // For tolower
#include <cstdio> // For remove
#include <cstdlib> // For system
#include <fstream>
#include <functional> // For bind
#include <iostream>
#include <iterator> // For back_inserter, ostream_iterator, istream_iterator
#include <sstream>
#include <numeric> // For accumulate
#include <stdexcept>
#include <string>
#include <vector>
class Analyze
{
protected:
	int voltThres, width, delta_pulse, drop_ratio_below, negate, start, peak, end, my_ratio, new_width, sum, begin, stop;
	double drop_ratio, ratio_test;
	std::string file_name;
	const int ONE = 1, TWO = 2, THREE = 3, FOUR = 4, FIFTEEN = 15, NEGONE = -1, MAXSIZE = 3070;
	std::string lineRead;
	std::vector<int> RoughGraph;
	std::vector<int> SmoothGraph;
	std::vector<int> Pulses;
	std::vector<int> Peaks;
	std::vector<int> GoodPulse;
	std::vector<int> PulseSums;
public:
	Analyze();
	~Analyze();
	void ReadData(std::string);
	void ReadINI(std::istream&);
	void Display();
	void CleanGraph();
	void ClearData();
	void FindPulse();
	void FindArea(int start, int end);
};

