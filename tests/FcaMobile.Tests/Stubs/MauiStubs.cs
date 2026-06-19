// Minimal stubs for MAUI platform APIs that are unavailable outside of a MAUI
// host process. These allow the service-layer source files to compile and run
// under a plain net8.0 xUnit test project.

// ReSharper disable once CheckNamespace
namespace Microsoft.Maui.Storage;

// Stub — not used by any service being tested, but referenced by CustomerStore.
// CustomerStore is NOT linked into the test project because it depends on
// SecureStorage and Preferences; it is tested via integration / device tests.
