// A module to build and release Heroes Profile Uploader

package main

import (
	"context"
	"dagger/heroes-profile-uploader/internal/dagger"
	"fmt"
	"strings"
)

type HeroesProfileUploader struct{}

func (m *HeroesProfileUploader) Build(ctx context.Context, git *Directory) (string, error) {
	return dag.Container().
		From("mcr.microsoft.com/dotnet/sdk:8.0").
		WithDirectory("/src", git, dagger.ContainerWithDirectoryOpts{
			Exclude: []string{"**/bin/**", "**/obj/**"},
		}).
		WithWorkdir("/src").
		WithExec([]string{
			"dotnet",
			"build"}).
		Stdout(ctx)
}

func (m *HeroesProfileUploader) Publish(ctx context.Context, git *Directory, version string) *dagger.Container {
	return dag.Container().
		From("mcr.microsoft.com/dotnet/sdk:8.0").
		WithDirectory("/src", git, dagger.ContainerWithDirectoryOpts{
			Exclude: []string{"**/bin/**", "**/obj/**"},
		}).
		WithWorkdir("/src/HeroesProfile.Uploader.Windows").
		WithExec([]string{
			"msbuild",
			"/t:publish",
			fmt.Sprintf("/property:ApplicationVersion=%s", version),
			"/p:PublishProfile=Properties/PublishProfiles/ClickOnceProfile.pubxml",
			"/p:PublishDir=\"/publish\""})
}

func (m *HeroesProfileUploader) Release(
	ctx context.Context,
	git *Directory,
	tag string,
	token *dagger.Secret) error {

	version := strings.TrimPrefix(tag, "v")
	publish := m.Publish(ctx, git.Directory("src"), version)
	fileNames, _ := publish.Directory("/publish").Entries(ctx)

	files := []*dagger.File{}

	for _, file := range fileNames {
		files = append(files, publish.File(file))
	}

	gh := dag.Gh(dagger.GhOpts{
		Token:  token,
		Repo:   "github.com/HeroesReplay/HeroesProfile.Uploader",
		Source: git,
	})

	_, err := gh.Release().Create(ctx, tag, "", dagger.GhReleaseCreateOpts{
		Target:        "master",
		Files:         files,
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
