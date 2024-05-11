package main

import (
	"encoding/json"
	"fmt"
	"log"
	"os"
	"os/exec"
	"strconv"
	"strings"
)

func main() {
	gitPath, err := exec.LookPath("git")
	if err != nil {
		log.Fatalf("failed to find git on path: %v", err)
	}

	cmd := exec.Command(gitPath, "rev-list", "master", "--count")
	output, err := cmd.Output()
	if err != nil {
		log.Fatalf("failed to run git: %v", err)
	}
	count, err := strconv.Atoi(strings.TrimSpace(string(output)))
	if err != nil {
		log.Fatalf("failed to parse git output: %v", err)
	}

	newVers := fmt.Sprintf("0.0.%d", count+1)
	// The current commit isn't the one we're about to commit.

	if err := modPackageJson(newVers); err != nil {
		log.Fatalf("%v\n", err)
	}

	if err := modRuntimeVersion(newVers); err != nil {
		log.Fatalf("%v\n", err)
	}

	verFile, err := os.Create(".version")
	if err != nil {
		log.Fatalf("failed to parse git output: %v", err)
	}
	verFile.WriteString(newVers)

	fmt.Println(newVers)
}

// load in the package.json
func modPackageJson(version string) error {
	packageSrc := "./cc.platoon.unity-sdk/package.json"

	data, err := os.ReadFile(packageSrc)
	if err != nil {
		return err
	}

	pkg := map[string]any{}
	if err := json.Unmarshal(data, &pkg); err != nil {
		return err
	}

	pkg["version"] = version

	b, _ := json.MarshalIndent(pkg, "", "\t")
	return os.WriteFile(packageSrc, b, 0755)
}

func modRuntimeVersion(version string) error {
	v := `namespace Platoon
{
	public class Version
	{
		public static string SDK = "unity %s";
	}
}`

	os.WriteFile("./cc.platoon.unity-sdk/Runtime/Version.cs", []byte(fmt.Sprintf(v, version)), 0755)
	return nil
}
