#include "XRef.h"
#include <iostream>
#include <fstream>
#include <ostream>
#include <sstream>
#include <string>
#include <map>
#include <vector>
#include <algorithm>
#include <iomanip>
using namespace std;
XRef::XRef(){}
XRef::~XRef(){}
struct LessThan
{
	bool operator() (const string& a, const string& b)
	{
		string lower_a, lower_b;
		lower_a = a; lower_b = b;
		transform(lower_a.begin(), lower_a.end(), lower_a.begin(), tolower);
		transform(lower_b.begin(), lower_b.end(), lower_b.begin(), tolower);
		if (lower_a == lower_b){return a < b;}
		else{return lower_a < lower_b;}
	}
};
string XRef::RemoveNonAlpha(string& line)
{
	for (size_t i = 0; i < line.size(); i++)
	{
		if (line[i] == '\''){}
		else if (!isalpha(line[i]))
		{
			line.replace(i, 1, " ");
		}
		else{}
	}
	return line;
}
void XRef::FileRead(string name)
{
	int linenumber = 1, count = 1;
	size_t largest_string = 0;
	map<string, map<int,int>, LessThan> MyMap;
	istringstream outstring;
	string temp = name;
	string line = "", temp_string = "", my_string = "";
	ifstream ReadFile(temp);
	ofstream OutFile;
	OutFile.open("MyFile.txt");
	if (ReadFile.fail()){cout << "Invalid file name" << endl;}
	else
	{
		while (ReadFile)
		{
			vector<string> MyVect;
			getline(ReadFile, line);
			line = XRef::RemoveNonAlpha(line);
			istringstream iss{ line };
			while (iss)
			{
				iss >> my_string;
				if (my_string == "")
				{
					break;
				}
				MyVect.push_back(my_string);
				my_string = "";
			}
			for (size_t i = 0; i < MyVect.size(); i++)
			{
				temp_string = MyVect[i];
				if (largest_string < temp_string.length()){largest_string = temp_string.length();}
				if (MyMap[temp_string][linenumber] == 0)
				{
					MyMap[temp_string][linenumber] = 1;
				}
				else
				{
					MyMap[temp_string][linenumber]++;
				}
			}
			MyVect.clear();
			++linenumber;
		}
		for (auto it : MyMap)
		{
			OutFile << it.first << setw(largest_string-it.first.size()+3) << " : ";
			for (auto il : it.second)
			{
				if (count == NINE)
				{
					OutFile << endl;
					OutFile << setw(largest_string+THREE) << " : ";
					count = 0;
				}
				OutFile << il.first << ":" << il.second << ", ";
				count++;
			}
			count = 0;
			OutFile << endl;
		}
	}
	MyMap.empty();
}