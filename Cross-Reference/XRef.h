#pragma once
#include <map>
#include <string>
class XRef
{
public:
	const int NINE = 9, THREE = 3;
	XRef();
	~XRef();
	void FileRead(std::string);
	std::string RemoveNonAlpha(std::string&);
};

