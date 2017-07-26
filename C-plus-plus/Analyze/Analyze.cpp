#include "Analyze.h"
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
using namespace std;

Analyze::Analyze()
{
	voltThres = 0, width = 0, delta_pulse = 0, drop_ratio_below = 0, negate = 0, end = 0, my_ratio = 0, new_width = 0, sum = 0;
	drop_ratio = 0.0, ratio_test = 0.0;
	file_name = "";
}
Analyze::~Analyze(){}
void Analyze::ReadData(string name)
{
	ifstream instrm;
	if (name == "gage2scope.ini")
	{
		instrm.open(name);
		if (instrm.fail())
		{
			cout << name << " failed to open." << endl;
		}
		string lineRead = "";
		if (instrm)
		{
			while (instrm)
			{
				getline(instrm, lineRead, '=');
				if (lineRead == "vt")
				{
					getline(instrm, lineRead);
					voltThres = stoi(lineRead);
				}
				else if (lineRead == "width")
				{
					getline(instrm, lineRead);
					width = stoi(lineRead);
				}
				else if (lineRead == "pulse_delta")
				{
					getline(instrm, lineRead);
					delta_pulse = stoi(lineRead);
				}
				else if (lineRead == "drop_ratio")
				{
					getline(instrm, lineRead);
					drop_ratio = stod(lineRead);
				}
				else if (lineRead == "below_drop_ratio")
				{
					getline(instrm, lineRead);
					drop_ratio_below = stoi(lineRead);
				}
			}
		}
	}
	else
	{
		instrm.open(name);
		if (instrm.fail())
		{
			cout << name << " failed to open." << endl;
		}
		while (instrm)
		{
			getline(instrm, lineRead);
			if (lineRead == "")
			{
				break;
			}
			negate = stoi(lineRead)*NEGONE;
			RoughGraph.push_back(negate);
		}
	}
}
void Analyze::Display()
{
	cout << voltThres << endl;
	cout << width << endl;
	cout << delta_pulse << endl;
	cout << drop_ratio << endl;
	cout << drop_ratio_below << endl;
}
void Analyze::CleanGraph()
{
	int temp = 0;
	for(int i = 0; i < THREE; i++)
	{
		SmoothGraph.push_back(0);
	}
	for (size_t i = 3; i < RoughGraph.size() - THREE; i++)
	{
		temp = ((RoughGraph[i - THREE] + (TWO * RoughGraph[i - TWO]) + (THREE * RoughGraph[i - ONE]) + (THREE * RoughGraph[i]) + (THREE * RoughGraph[i + ONE]) + (TWO * RoughGraph[i + TWO]) + (RoughGraph[i + THREE])) / FIFTEEN);
		SmoothGraph.push_back(temp);
	}
	for (int i = 0; i < THREE; i++)
	{
		SmoothGraph.push_back(0);
	}
}
void Analyze::ClearData()
{
	while (RoughGraph.size() != 0)
	{
		RoughGraph.pop_back();
	}
	while (SmoothGraph.size() != 0)
	{
		SmoothGraph.pop_back();
	}
	while (Pulses.size() != 0)
	{
		Pulses.pop_back();
	}
	while (Peaks.size() != 0)
	{
		Peaks.pop_back();
	}
	while (GoodPulse.size() != 0)
	{
		GoodPulse.pop_back();
	}
	while (PulseSums.size() != 0)
	{
		PulseSums.pop_back();
	}
}
void Analyze::FindPulse()
{
	size_t spot = 0;
	new_width = width + ONE;
	for (int i = 0; i < MAXSIZE; i++)
	{
		if ((SmoothGraph[i + 2] - SmoothGraph[i]) > voltThres)
		{
			start = i;
			Pulses.push_back(start);
			i = i + 2;
			while (SmoothGraph[i] < SmoothGraph[i + 1])
			{
				i++;
			}
			peak = i;
			Peaks.push_back(peak);
		}
	}
	while (spot <= Pulses.size())
	{
		if (spot == Pulses.size()-1)
		{
			start = Pulses[spot];
			if ((SmoothGraph.size() - SmoothGraph[start]) <= width)
			{
				GoodPulse.push_back(Pulses[spot]);
				end = SmoothGraph.size();
				Analyze::FindArea(Pulses[spot], end);
			}
			else
			{
				GoodPulse.push_back(Pulses[spot]);
				Analyze::FindArea(Pulses[spot], Pulses[spot]+new_width);
			}
			break;
		}
		else if ((Pulses[spot + 1] - Pulses[spot]) <= delta_pulse)
		{
			peak = Peaks[spot];
			start = Peaks[spot]+1;
			end = Pulses[spot+1];
			ratio_test = drop_ratio*SmoothGraph[peak];
			while (start < end)
			{
				if (SmoothGraph[start] < ratio_test)
				{
					my_ratio++;
				}
				start++;
			}
			if (my_ratio > drop_ratio_below)
			{
				spot++;
			}
			else
			{
				GoodPulse.push_back(Pulses[spot]);
				start = Pulses[spot];
				end = Pulses[spot + 1]; 
				Analyze::FindArea(start, end);
				spot++;
			}
			my_ratio = 0;
		}
		else if ((Pulses[spot + 1] - Pulses[spot]) <= width)
		{
			start = Pulses[spot];
			spot++;
		}
		else
		{
			GoodPulse.push_back(Pulses[spot]);
			Analyze::FindArea(Pulses[spot], width);
			spot++;
		}
	}
	for (size_t i = 0; i < GoodPulse.size(); i++)
	{
		cout << GoodPulse[i] << " ";
	}
	cout << endl;
	for (size_t i = 0; i < PulseSums.size(); i++)
	{
		cout << PulseSums[i] << " ";
	}
}
void Analyze::FindArea(int start, int end)
{
	sum = 0;
	begin = start;
	stop = end;
	if ((stop - begin) < width)
	{
		while (begin < stop)
		{
			sum += RoughGraph[begin];
			begin++;
		}
	}
	else
	{
		while (begin < stop-1)
		{
			sum += RoughGraph[begin];
			begin++;
		}
	}
	PulseSums.push_back(sum);
}