#define _CRT_SECURE_NO_WARNINGS
#include "Employee.h"
#include <stdexcept>
#include <iostream>
#include <string>
using namespace std;


struct EmployeeRec
{
	int id;
	char name[31];
	char address[26];
	char city[21];
	char state[21];
	char country[21];
	char phone[21];
	double salary;
};
const int NAME = 31, ADDRESS = 26, CITY = 21, STATE = 21, COUNTRY = 21, PHONE = 21;
const string EMPTAG = "<Employee>";
const string SALARYTAG = "<salary>";
const string ADDRESSTAG = "<address>";
const string CITYTAG = "<city>";
const string STATETAG = "<state>";
const string COUNTRYTAG = "<country>";
const string PHONETAG = "<phone>";
const string IDTAG = "<id>";
const string NAMETAG = "<name>";
const string _EMPTAG = "</Employee>";
const string _SALARYTAG = "</salary>";
const string _ADDRESSTAG = "</address>";
const string _CITYTAG = "</city>";
const string _STATETAG = "</state>";
const string _COUNTRYTAG = "</country>";
const string _PHONETAG = "</phone>";
const string _IDTAG = "</id>";
const string _NAMETAG = "</name>";
const string SECONDEMPTAG = " <Employee>";
const string THIRDEMPTAG = "\n<Employee>";

Employee::Employee()
{
	name = "";
	id = 0;
	address = "";
	city = "";
	state = "";
	country = "";
	phone = "";
	salary = 0.0;
}
Employee::~Employee()
{
}
Employee::Employee(int iden, string na, string ad = "", string ci = "", string st = "", string co = "", string ph = "", double sa = 0.0)
{
	id = iden;
	name = na;
	address = ad;
	city = ci;
	state = st;
	country = co;
	phone = ph;
	salary = sa;
}
void Employee::Create(int iden, string na, string ad, string ci, string st, string co, string ph, double sa)
{
	id = iden;
	name = na;
	address = ad;
	city = ci;
	state = st;
	country = co;
	phone = ph;
	salary = sa;
}
void Employee::display(std::ostream& ostrm)const // Write a readable Employee representation to a stream
{
	ostrm << "ID: " << id << endl;
	ostrm << "Name: " << name << endl;
	ostrm << "Address: " << address << endl;
	ostrm << "City : " << city << endl;
	ostrm << "State: " << state << endl;
	ostrm << "Country: " << country << endl;
	ostrm << "Phone: " << phone << endl;
	ostrm << "Salary: " << salary << endl;

}
void Employee::write(std::ostream& ostrm) const // Write a fixed-length record to current file position
{
	EmployeeRec myEmpRec;
	myEmpRec.id = id;
	strncpy(myEmpRec.name, name.c_str(), 30)[30] = '\0';
	strncpy(myEmpRec.address, address.c_str(), 25)[25] = '\0';
	strncpy(myEmpRec.city, city.c_str(), 20)[20] = '\0';
	strncpy(myEmpRec.state, state.c_str(), 20)[20] = '\0';
	strncpy(myEmpRec.country, country.c_str(), 20)[20] = '\0';
	strncpy(myEmpRec.phone, phone.c_str(), 20)[20] = '\0';
	myEmpRec.salary = salary;
	ostrm.write(reinterpret_cast<char*>(&myEmpRec), sizeof myEmpRec);
	ostrm.flush();
}
void Employee::store(std::iostream& iostrm) const // Overwrite (or append) record in (to) file
{
	bool found = false;
	int i = 0;
	const int MAXRECORDS = 3;
	EmployeeRec myEmpRec;
	myEmpRec.id = id;
	strncpy(myEmpRec.name, name.c_str(), 30)[30] = '\0';
	strncpy(myEmpRec.address, address.c_str(), 25)[25] = '\0';
	strncpy(myEmpRec.city, city.c_str(), 20)[20] = '\0';
	strncpy(myEmpRec.state, state.c_str(), 20)[20] = '\0';
	strncpy(myEmpRec.country, country.c_str(), 20)[20] = '\0';
	strncpy(myEmpRec.phone, phone.c_str(), 20)[20] = '\0';
	myEmpRec.salary = salary;
	iostrm.clear();
	iostrm.seekg(0, ios::beg);
	auto p = iostrm.tellg();
	Employee* TempEmp = Employee::read(iostrm);
	//once emp is found, clear stream, get to head of emp and overwrite
	//if not there, do write to iostrm
	while (TempEmp != nullptr)
	{
		if (TempEmp->id == myEmpRec.id)
		{
			iostrm.clear();
			iostrm.seekg(p);
			Employee::write(iostrm);
			found = true;
			break;
		}
		i++;
		p = iostrm.tellg();
		TempEmp = Employee::read(iostrm);
		if (i == MAXRECORDS)
		{
			TempEmp = nullptr;
		}
	}
	if (found == false)
	{
		iostrm.clear();
		Employee::write(iostrm);
	}
}
void Employee::toXML(std::ostream& ostrm) const // Write XML record for Employee
{
	ostrm << EMPTAG << "\n";
	ostrm << "\t" << IDTAG;
	ostrm << id;
	ostrm << _IDTAG <<"\n";
	ostrm << "\t" << NAMETAG;
	ostrm << name;
	ostrm << _NAMETAG << "\n";
	ostrm << "\t" << ADDRESSTAG;
	ostrm << address;
	ostrm << _ADDRESSTAG << "\n";
	ostrm << "\t" << CITYTAG;
	ostrm << city;
	ostrm << _CITYTAG << "\n";
	ostrm << "\t" << STATETAG;
	ostrm << state;
	ostrm << _STATETAG << "\n";
	ostrm << "\t" << COUNTRYTAG;
	ostrm << country;
	ostrm << _COUNTRYTAG << "\n";
	ostrm << "\t" << PHONETAG;
	ostrm << phone;
	ostrm << _PHONETAG << "\n";
	ostrm << "\t" << SALARYTAG;
	ostrm << salary;
	ostrm << _SALARYTAG << "\n";
	ostrm << _EMPTAG << "\n";
}
Employee* Employee::read(std::istream& instrm) // Read record from current file position
{
	EmployeeRec myEmpRec;
	instrm.read(reinterpret_cast<char*>(&myEmpRec), sizeof myEmpRec);
	Employee* TempEmp = new Employee();
	if (instrm)
	{
		TempEmp->id = myEmpRec.id;
		TempEmp->name = myEmpRec.name;
		TempEmp->address = myEmpRec.address;
		TempEmp->city = myEmpRec.city;
		TempEmp->state = myEmpRec.state;
		TempEmp->country = myEmpRec.country;
		TempEmp->phone = myEmpRec.phone;
		TempEmp->salary = myEmpRec.salary;
	}
	return TempEmp;
}
Employee* Employee::retrieve(std::istream& instrm, int find) // Search file for record by id
{
	EmployeeRec myEmpRec;
	instrm.clear();
	instrm.seekg(0, ios::beg);
	//instrm.read(reinterpret_cast<char*>(&myEmpRec), sizeof myEmpRec);
	Employee* TempEmp = Employee::read(instrm);
	while (TempEmp != nullptr)
	{
		if (TempEmp->id == find)
		{
			return TempEmp;
		}
		TempEmp = Employee::read(instrm);
	}
	return nullptr;
}
Employee* Employee::fromXML(std::istream& instrm)// Read the XML record from a stream
{
	string temp = "", tempNa = "", tempSal = "", tempID = "", tempAdd = "", tempCi = "", tempSt = "", tempCo = "", tempPh = "";
	bool endofemp = false, extracity = false;
	int passedID = 0;
	double passedSalary = 0.0;
	temp = GetTag(instrm);
	if (temp == SECONDEMPTAG)
	{
		temp = temp.substr(temp.find_first_of('<'), temp.length());
	}
	if (temp == THIRDEMPTAG)
	{
		temp = temp.substr(temp.find_first_of('<'), temp.length() -1);
	}
	while (endofemp != true)
	{
		if (temp == "\n\n")
		{
			return nullptr;
		}
		else if (temp == EMPTAG)
		{
			temp = GetTag(instrm);
			temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
		}
		else if (temp == SALARYTAG)
		{
			tempSal = GetValue(instrm);
			passedSalary = stof(tempSal);
			temp = GetTag(instrm);
			if (temp != _SALARYTAG)
			{
				throw runtime_error("Missing a ending salary tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
			}
		}
		else if (temp == NAMETAG)
		{
			tempNa = GetValue(instrm);
			temp = GetTag(instrm);
			if (temp != _NAMETAG)
			{
				throw runtime_error("Missing an ending name tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
			}
		}
		else if (temp == IDTAG)
		{
			tempID = GetValue(instrm);
			passedID = stoi(tempID);
			temp = GetTag(instrm);
			if (temp != _IDTAG)
			{
				throw runtime_error("Missing an ending ID tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
			}
		}
		else if (temp == ADDRESSTAG)
		{
			tempAdd = GetValue(instrm);
			temp = GetTag(instrm);
			if (temp != _ADDRESSTAG)
			{
				throw runtime_error("Missing an ending address tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
			}
		}
		else if (temp == CITYTAG)
		{
			if (extracity == true)
			{
				throw runtime_error("City already found");
				return nullptr;
			}
			tempCi = GetValue(instrm);
			temp = GetTag(instrm);
			if (temp != _CITYTAG)
			{
				throw runtime_error("Missing an ending city tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
				extracity = true;
			}
		}
		else if (temp == STATETAG)
		{
			tempSt = GetValue(instrm);
			temp = GetTag(instrm);
			if (temp != _STATETAG)
			{
				throw runtime_error("Missing an ending state tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
			}
		}
		else if (temp == COUNTRYTAG)
		{
			tempCo = GetValue(instrm);
			temp = GetTag(instrm);
			if (temp != _COUNTRYTAG)
			{
				throw runtime_error("Missing an ending country tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
			}
		}
		else if (temp == PHONETAG)
		{
			tempPh = GetValue(instrm);
			temp = GetTag(instrm);
			if (temp != _PHONETAG)
			{
				throw runtime_error("Missing an ending phone tag");
				return nullptr;
			}
			else
			{
				temp = GetTag(instrm);
				temp = temp.substr(temp.find_first_of('<'), temp.length() - 1);
			}
		}
		else if (temp == _EMPTAG)
		{
			if (tempNa == "")
			{
				throw runtime_error("Missing name");
				return nullptr;
			}
			else if (tempID == "")
			{
				throw runtime_error("Missing ID");
				return nullptr;
			}
			else
			{
				Employee* TempEmp = new Employee(passedID,tempNa,tempAdd,tempCi,tempSt,tempCo,tempPh,passedSalary);
				return TempEmp;
				endofemp = true;
			}
		}
		else
		{
			throw runtime_error("Missing ending employee tag");
		}
	}
}
string Employee::GetTag(std::istream& instrm)//This gets the next tag from a stream
{
	string tempString = "";
	getline(instrm, tempString, '>');
	instrm.unget();
	tempString += instrm.get();
	return tempString;
}
string Employee::GetValue(std::istream& instrm)//This gets the data between the tags
{
	string tempString = "";
	getline(instrm, tempString, '<');
	instrm.unget();
	return tempString;
}
void Employee::Change(double newSal)
{
	salary = newSal;
}
void Employee::GetSalary(std::ostream& ostrm)
{
	ostrm << salary;
}