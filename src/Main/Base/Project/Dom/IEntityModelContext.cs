﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.SharpDevelop.Parser;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Refactoring;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// The context for an entity model.
	/// This may be a reference to a project, or a compilation provider for a single stand-alone code file.
	/// </summary>
	public interface IEntityModelContext
	{
		/// <summary>
		/// Used for <see cref="IEntityModel.ParentProject"/>.
		/// </summary>
		IProject Project { get; }
		
		/// <summary>
		/// Used for <see cref="IEntityModel.Resolve()"/>.
		/// </summary>
		/// <param name="solutionSnapshot">
		/// The solution snapshot provided to <see cref="IEntityModel.Resolve(ISolutionSnapshotWithProjectMapping)"/>,
		/// or null if the <see cref="IEntityModel.Resolve()"/> overload was used.
		/// </param>
		ICompilation GetCompilation();
		
		/// <summary>
		/// Returns true if part1 is considered a better candidate for the primary part than part2.
		/// </summary>
		bool IsBetterPart(IUnresolvedTypeDefinition part1, IUnresolvedTypeDefinition part2);
		
		/// <summary>
		/// Short name of current assembly.
		/// </summary>
		string AssemblyName { get; }
		
		/// <summary>
		/// Full path and file name of the assembly. Output assembly for projects.
		/// </summary>
		string Location { get; }
	}
	
	public class ProjectEntityModelContext : IEntityModelContext
	{
		readonly IProject project;
		readonly string primaryCodeFileExtension;
		
		public ProjectEntityModelContext(IProject project, string primaryCodeFileExtension)
		{
			if (project == null)
				throw new ArgumentNullException("project");
			this.project = project;
			this.primaryCodeFileExtension = primaryCodeFileExtension;
		}
		
		public string AssemblyName {
			get { return project.AssemblyName; }
		}
		
		public string Location {
			get { return project.OutputAssemblyFullPath; }
		}
		
		public IProject Project {
			get { return project; }
		}
		
		public ICompilation GetCompilation()
		{
			return SD.ParserService.GetCompilation(project);
		}
		
		public bool IsBetterPart(IUnresolvedTypeDefinition part1, IUnresolvedTypeDefinition part2)
		{
			return EntityModelContextUtils.IsBetterPart(part1, part2, primaryCodeFileExtension);
		}
	}
	
	public class AssemblyEntityModelContext : IEntityModelContext
	{
		ICompilation compilation;
		IUnresolvedAssembly mainAssembly;
		IAssemblyReference[] references;
		
		public AssemblyEntityModelContext(IUnresolvedAssembly mainAssembly, params IAssemblyReference[] references)
		{
			if (mainAssembly == null)
				throw new ArgumentNullException("mainAssembly");
			this.mainAssembly = mainAssembly;
			this.references = references;
			// implement lazy init + weak caching
			this.compilation = new SimpleCompilation(mainAssembly, references);
		}
		
		public string AssemblyName {
			get { return mainAssembly.AssemblyName; }
		}
		
		public string Location {
			get { return mainAssembly.Location; }
		}
		
		public ICompilation GetCompilation()
		{
			return compilation;
		}
		
		public bool IsBetterPart(IUnresolvedTypeDefinition part1, IUnresolvedTypeDefinition part2)
		{
			return false;
		}
		
		public IProject Project {
			get { return null; }
		}
	}
	
	public static class EntityModelContextUtils
	{
		public static bool IsBetterPart(IUnresolvedTypeDefinition part1, IUnresolvedTypeDefinition part2, string codeFileExtension)
		{
			IUnresolvedFile file1 = part1.UnresolvedFile;
			IUnresolvedFile file2 = part2.UnresolvedFile;
			if (file1 != null && file2 == null)
				return true;
			if (file1 == null)
				return false;
			bool file1HasExtension = file1.FileName.EndsWith(codeFileExtension, StringComparison.OrdinalIgnoreCase);
			bool file2HasExtension = file2.FileName.EndsWith(codeFileExtension, StringComparison.OrdinalIgnoreCase);
			if (file1HasExtension && !file2HasExtension)
				return true;
			if (!file1HasExtension && file2HasExtension)
				return false;
			return file1.FileName.Length < file2.FileName.Length;
		}
	}
}
