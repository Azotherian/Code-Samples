#pragma once
#include <string>
#include <iostream>
#include <memory>
#define _CRT_SECURE_NO_WARNINGS
using std::string;
class Employee
{
protected:
	string name;
	int id;
	string address;
	string city;
	string state;
	string country;
	string phone;
	double salary;

public:
	void display(std::ostream&) const; // Write a readable Employee representation to a stream
	void write(std::ostream&) const; // Write a fixed-length record to current file position

	//overwrites the record if it exists already; otherwise it appends a new record to the file.
	void store(std::iostream&) const; // Overwrite (or append) record in (to) file

	void toXML(std::ostream&) const; // Write XML record for Employee

	//return a null pointer if failed to read valid input on these functions:
	static Employee* read(std::istream&); // Read record from current file position
	static Employee* retrieve(std::istream&, int); // Search file for record by id
	static Employee* fromXML(std::istream&); // Read the XML record from a stream

	/*Throw runtime_error exceptions with a suitable message if any required XML tags are missing, or if any end tags for existing start tags
	are missing, or for any other abnormalities.
	You might consider using unique_ptr to prevent memory leaks.*/
	Employee();
	~Employee();
	Employee(int iden, string na, string ad, string ci, string st, string co, string ph, double sa);
	void Create(int id, string na, string ad, string ci, string st, string co, string ph, double sa);
	static string GetTag(std::istream&); //This gets the next tag from a stream
	static string GetValue(std::istream&); //This gets the data between the tags
	void Change(double newSal);
	void GetSalary(std::ostream&);
};

