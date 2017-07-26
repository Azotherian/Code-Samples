#include "Employee.h"
#include <iostream>
#include <fstream>
#include <ostream>
#include <sstream>
#include <string>
#include <vector>
#include <memory>
using namespace std;

int main(int argc, char* argv[])
{
	try
	{
		string temp = "";
		string temptag = "";
		vector <unique_ptr<Employee>> EmpVect;
		fstream ReadFile;
		Employee* TempEmp;
		fstream OutFile;
		const int FILLED = 3;
		//1) Obtain the name of an XML file to read from the command line(argv[1]).Print an error message and halt
		//the program if there is no command - line argument provided.
		ReadFile.open(argv[1]);
		if (ReadFile.fail())
		{
			cout << "Invalid file name." << endl;
		}
		//2) Read each XML record in the file by repeatedly calling Employee::fromXML, which creates an Employee
		//object on - the - fly, and store it in a vector of Employee pointers(you may use smart pointers).
		while (EmpVect.capacity() < FILLED)
		{
			TempEmp = Employee::fromXML(ReadFile);
			if (TempEmp != nullptr)
			{
				EmpVect.push_back(unique_ptr<Employee>(TempEmp));
			}
		}
		//3) Loop through your vector and print to cout the Employee data for each object(using the display member
		//function).
		for (size_t i = 0; i < EmpVect.size(); ++i)
		{
			EmpVect[i]->display(cout);
			cout << endl;
		}
		//4) next step, create a new file of fixed-length Employee records. Write the records for each employee to your new file(call it “employee.bin”) in the order they appear in your vector.
		OutFile.open("employee.bin", ios::in | ios::out | ios::binary | ios::trunc);
		if (OutFile.is_open())
		{
			for (size_t i = 0; i < EmpVect.size(); ++i)
			{
				EmpVect[i]->write(OutFile);
			}
		}
		else
		{
			cout << "employee.bin is not open" << endl;
		}
		//5) Clear your vector in preparation for the next step.
		for (size_t i = 0; i != EmpVect.size();)
		{
			EmpVect.pop_back();
		}
		//6) Traverse the file by repeated calls to Employee::read, filling your newly emptied vector with Employee pointers for each record read.
		OutFile.clear();
		OutFile.seekg(0, ios::beg);
		for (int i = 0; i < FILLED; ++i)
		{
			TempEmp = Employee::read(OutFile);
			if (TempEmp != nullptr)
			{
				EmpVect.push_back(unique_ptr<Employee>(TempEmp));
			}
		}
		//7) Loop through your vector and print to cout an XML representation of each Employee using Employee::toXML.
		for (size_t i = 0; i < EmpVect.size(); i++)
		{
			EmpVect[i]->toXML(cout);
		}
		//8) Search the file for the Employee with id 12345 using Employee::retrieve.
		OutFile.clear();
		OutFile.seekg(0, ios::beg);
		TempEmp = Employee::retrieve(OutFile, 12345);
		cout << "\nFound: " << endl;
		TempEmp->display(cout);
		//9) Change the salary for the retrieved object to 150000.0
		TempEmp->Change(150000.0);
		//10) Write the modified Employee object back to file using Employee::store
		TempEmp->store(OutFile);
		//11) Retrieve the object again by id(12345) and print its salary to verify that the file now has the updated salary.
		OutFile.clear();
		OutFile.seekg(0, ios::beg);
		TempEmp->retrieve(OutFile, 12345);
		cout << "New Salary: ";
		TempEmp->GetSalary(cout);
		cout << endl;
		//12) Create a new Employee object of your own with a new, unique id, along with other information.
		Employee* MyEmp = new Employee(2468, "Bob Ross", "123 S. 111 W", "Orem", "Utah", "USA", "801-555-5555", 29000.00);
		//13) Store it in the file using Employee::store.
		MyEmp->store(OutFile);
		//14) Retrieve the record with Employee::retrieve and display it to cout.
		MyEmp->retrieve(OutFile, 2468);
		cout << endl;
		MyEmp->display(cout);
	}
	catch (runtime_error(exceptthrow))
	{
		cout << exceptthrow.what() << endl;
	}
	system("PAUSE");
	return 0;
}

//Expected Output:
/*id: 1234
name: John Doe
address: 2230 W. Treeline Dr.
city: Tucson
state: Arizona
country: USA
phone: 520-742-2448
salary: 40000
id: 4321
name: Jane Doe
address:
city:
state:
country:
phone:
salary: 60000
id: 12345
name: Jack Dough
address: 24437 Princeton
city: Dearborn
state: Michigan
country: USA
phone: 303-427-0153
salary: 140000
<Employee>
<Name>John Doe</Name>
<ID>1234</ID>
<Address>2230 W. Treeline Dr.</Address>
<City>Tucson</City>
<State>Arizona</State>
<Country>USA</Country>
<Phone>520-742-2448</Phone>
<Salary>40000</Salary>
</Employee>
<Employee>
<Name>Jane Doe</Name>
<ID>4321</ID>
<Salary>60000</Salary>
</Employee>
<Employee>
<Name>Jack Dough</Name>
<ID>12345</ID>
<Address>24437 Princeton</Address>
<City>Dearborn</City>
<State>Michigan</State>
<Country>USA</Country>
<Phone>303-427-0153</Phone>
<Salary>140000</Salary>
</Employee>
Found:
id: 12345
name: Jack Dough
address: 24437 Princeton
city: Dearborn
state: Michigan
country: USA
phone: 303-427-0153
salary: 140000
150000*/