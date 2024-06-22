// A module to build and release Heroes Profile Uploader

package main

import (
	"context"
	"dagger/heroes-profile-uploader/internal/dagger"
	"fmt"
	"strings"
)

type HeroesProfileUploader struct {
}

func (m *HeroesProfileUploader) Build(ctx context.Context, git *Directory) *dagger.Container {
	return dag.Container().
		From("mcr.microsoft.com/dotnet/sdk:8.0").
		WithDirectory("/src", git, dagger.ContainerWithDirectoryOpts{
			Exclude: []string{"**/bin/**", "**/obj/**"},
		}).
		WithWorkdir("/src").
		WithExec([]string{"dotnet", "build", "-c", "Release"})
}

/*
Well that sucks, it seems like mage is working on windows only. That means we need to build the clickonce application on windows.


https://learn.microsoft.com/en-us/visualstudio/deployment/quickstart-deploy-using-clickonce-folder?view=vs-2022
https://learn.microsoft.com/en-us/visualstudio/deployment/building-dotnet-clickonce-applications-from-the-command-line?view=vs-2022


How to publish clickonce to Github

https://janjones.me/posts/clickonce-installer-build-publish-github/
Example: https://github.com/janarez/inoculus

*/

// dotnet publish ./Heroesprofile.Uploader.Windows/ --self-contained --os win -o ./publish
// dotnet mage -al Heroesprofile.Uploader.exe -TargetDirectory publish
// dotnet mage -new Application -t .\publish\Heroesprofile.Uploader.manifest -FromDirectory /publish -v 1.0.0.0 -IconFile ./Heroesprofile.Uploader.Windows/Resources/uploader_icon_dark.ico
// dotnet mage -new Deployment -Install true -Publisher "Patrick Magee" -v 1.0.0.0 -AppManifest .\publish\Heroesprofile.Uploader.manifest -t Heroesprofile.Uploader.application

func (m *HeroesProfileUploader) ClickOnce(ctx context.Context, git *Directory, version string) *dagger.Container {
	return m.Publish(ctx, git, version).
		WithExec([]string{"dotnet", "tool", "install", "microsoft.dotnet.mage", "--global", "--version", "8.0.0"}).
		WithExec([]string{"/root/.dotnet/tools/dotnet-mage", "-al", "Heroesprofile.Uploader.exe", "-TargetDirectory", "/publish"}).
		WithExec([]string{"/root/.dotnet/tools/dotnet-mage", "-new", "Application", "-t", "/publish/Heroesprofile.Uploader.manifest", "-FromDirectory", "/publish", "-v", version, "-IconFile", "/publish/Resources/uploader_icon_dark.ico"}).
		WithExec([]string{"/root/.dotnet/tools/dotnet-mage", "-new", "Deployment", "-Install", "true", "-Publisher", "Patrick Magee", "-v", version, "-AppManifest", "/publish/Heroesprofile.Uploader.manifest", "-t", "Heroesprofile.Uploader.application"})
}

func (m *HeroesProfileUploader) Publish(ctx context.Context, git *Directory, version string) *dagger.Container {
	return m.Build(ctx, git).
		WithWorkdir("/src").
		WithExec([]string{"mkdir", "/publish"}).
		WithExec([]string{"dotnet", "publish", "Heroesprofile.Uploader.Windows", "--self-contained", "--os", "win", "-o", "/publish"})
}

func (m *HeroesProfileUploader) Release(
	ctx context.Context,
	git *Directory,
	tag string,
	token *dagger.Secret) error {

	version := strings.TrimPrefix(tag, "v")
	publish := m.Publish(ctx, git, version)
	assets := publish.Directory("/publish")

	// fileNames, _ := assets.Entries(ctx)

	// files := []*dagger.File{}

	// for _, file := range fileNames {
	// 	files = append(files, publish.File(file))
	// }

	zip := dag.Arc().ArchiveDirectory(fmt.Sprintf("HeroesProfile.Uploader_%s_windows_amd64", tag), assets).Zip()

	gh := dag.Gh(dagger.GhOpts{
		Token:  token,
		Repo:   "github.com/HeroesReplay/HeroesProfile.Uploader",
		Source: git,
	})

	_, err := gh.Release().Create(ctx, tag, "", dagger.GhReleaseCreateOpts{
		Target:        "dotnet-upgrade",
		Files:         []*dagger.File{zip},
		Latest:        true,
		VerifyTag:     true,
		Draft:         true,
		GenerateNotes: true,
	})

	if err != nil {
		return err
	}

	return nil
}
