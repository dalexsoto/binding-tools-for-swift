﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using SwiftReflector.SwiftXmlReflection;
using System.Linq;
using SwiftReflector;
using SwiftReflector.SwiftInterfaceReflector;
using SwiftReflector.TypeMapping;
using tomwiftytest;

namespace XmlReflectionTests {
	[TestFixture]
	public class SwiftInterfaceParserTests {

		static TypeDatabase typeDatabase;

		static SwiftInterfaceParserTests ()
		{
			typeDatabase = new TypeDatabase ();
			foreach (var dbPath in Compiler.kTypeDatabases) {
				if (!Directory.Exists (dbPath))
					continue;
				foreach (var dbFile in Directory.GetFiles (dbPath, "*.xml")) {
					typeDatabase.Read (dbFile);
				}
			}
		}
			

		void CompileStringToModule (string code, string moduleName)
		{
			Utils.CompileSwift (code, moduleName: moduleName);
		}

		XDocument ReflectToXDocument (string code, string moduleName, out SwiftInterfaceReflector reflector)
		{
			var compiler = Utils.CompileSwift (code, moduleName: moduleName);
			var files = Directory.GetFiles (compiler.DirectoryPath);
			var file = files.FirstOrDefault (name => name.EndsWith (".swiftinterface", StringComparison.Ordinal));
			if (file == null)
				Assert.Fail ("no swiftinterface file");
			reflector = new SwiftInterfaceReflector (typeDatabase);
			return reflector.Reflect (file);
		}

		List<ModuleDeclaration> ReflectToModules (string code, string moduleName, out SwiftInterfaceReflector reflector)
		{
			return Reflector.FromXml (ReflectToXDocument (code, moduleName, out reflector));
		}

		[Test]
		public void SimplestImportTest ()
		{
			var swiftCode = @"
import Swift
public func hello ()
{
    print (""hello"")
}
";
			SwiftInterfaceReflector reflector;
			var modules = ReflectToXDocument (swiftCode, "SomethingSomething", out reflector);

			var importModules = reflector.ImportModules;
			Assert.AreEqual (1, importModules.Count, "not 1 import module");
			Assert.AreEqual ("Swift", importModules [0], "not swift import module");
		}

		[Test]
		public void SimpleImportTest ()
		{
			var swiftCode = @"
import Swift
import Foundation

public func hello ()
{
    print (""hello"")
}
";
			SwiftInterfaceReflector reflector;
			var modules = ReflectToXDocument (swiftCode, "SomethingSomething", out reflector);

			var importModules = reflector.ImportModules;
			Assert.AreEqual (2, importModules.Count, "not 2 import modules");
			Assert.IsNotNull (importModules.FirstOrDefault (s => s == "Swift"), "no Swift import module");
			Assert.IsNotNull (importModules.FirstOrDefault (s => s == "Foundation"), "no Foundation import module");
		}

		[Test]
		public void TypeDatabaseHasOperators ()
		{
			var operators = typeDatabase.OperatorsForModule ("Swift");
			Assert.Less (0, operators.Count (), "no operators");
		}

		[Test]
		public void HasGlobalOperator ()
		{
			var swiftCode = @"
import Swift
public class Imag {
	public var Real:Float = 0, Imaginary: Float = 0

	public static func == (lhs: Imag, rhs: Imag) -> Bool {
		return lhs.Real == rhs.Real && lhs.Imaginary == rhs.Imaginary
	}
}
";

			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Imag");
			Assert.IsNotNull (cl, "no class");

			var fn = cl.Members.FirstOrDefault (m => m.Name == "==") as FunctionDeclaration;
			Assert.IsNotNull (fn, "no function");

			Assert.IsTrue (fn.IsOperator, "not an operator");
			Assert.AreEqual (OperatorType.Infix, fn.OperatorType, "wrong operator type");
		}

		[Test]
		public void HasLocalOperator ()
		{
			var swiftCode = @"
postfix operator *^*

public class Imag {
    public var Real:Float = 0, Imaginary: Float = 0
    
    public static postfix func *^* (lhs: Imag) -> Float {
        return 2 * lhs.Real
    }
}";
			SwiftInterfaceReflector reflector;
			var module = ReflectToModules (swiftCode, "SomeModule", out reflector).FirstOrDefault (m => m.Name == "SomeModule");

			Assert.IsNotNull (module, "no module");

			var cl = module.Classes.FirstOrDefault (c => c.Name == "Imag");
			Assert.IsNotNull (cl, "no class");

			var fn = cl.Members.FirstOrDefault (m => m.Name == "*^*") as FunctionDeclaration;
			Assert.IsNotNull (fn, "no function");

			Assert.IsTrue (fn.IsOperator, "not an operator");
			Assert.AreEqual (OperatorType.Postfix, fn.OperatorType, "wrong operator type");
		}
	}
}
