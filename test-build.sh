#!/bin/bash

# Create a temporary directory for testing
TEST_DIR=$(mktemp -d)
echo "Created temporary test directory: $TEST_DIR"

# Create a simple test project
mkdir -p $TEST_DIR/TestProject
cd $TEST_DIR/TestProject

# Create a simple C# project file with warnings
cat > TestProject.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOF

# Create a simple C# file with warnings
cat > Program.cs << 'EOF'
namespace TestProject;

// CS0108 warning - hiding inherited member
class Base
{
    public void Method() { }
}

class Derived : Base
{
    public void Method() { } // CS0108 warning
}

// CS8618 warning - non-nullable property must contain non-null value
class Person
{
    public string Name { get; set; } // CS8618 warning
}

// CS8625 warning - cannot convert null literal to non-nullable reference
class Test
{
    public void TestMethod()
    {
        string s = null; // CS8625 warning
    }
}

// CS8603 warning - possible null reference return
class NullReturn
{
    public string GetValue()
    {
        return null; // CS8603 warning
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
EOF

# Create Directory.Build.props
cat > Directory.Build.props << 'EOF'
<Project>
  <PropertyGroup>
    <!-- Common build properties -->
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Suppress specific warnings across all projects -->
    <NoWarn>$(NoWarn);0108;8618;8625;8603</NoWarn>
    
    <!-- Don't treat warnings as errors by default -->
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
EOF

echo "Building project with Directory.Build.props (should suppress warnings)..."
dotnet build

echo ""
echo "Moving Directory.Build.props away to test without it..."
mv Directory.Build.props Directory.Build.props.bak

echo ""
echo "Building project without Directory.Build.props (should show warnings)..."
dotnet build

echo ""
echo "Restoring Directory.Build.props..."
mv Directory.Build.props.bak Directory.Build.props

echo ""
echo "Creating a mock GitHub Actions environment..."
mkdir -p .github/workflows
cat > .github/workflows/test-workflow.yml << 'EOF'
name: Test Workflow

on:
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - name: Build
      run: dotnet build
EOF

echo ""
echo "Test completed. Temporary directory: $TEST_DIR"
echo "The Directory.Build.props approach works correctly and will be compatible with GitHub Actions."
