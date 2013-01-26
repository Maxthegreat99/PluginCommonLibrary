﻿// This file is provided unter the terms of the 
// Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/3.0/.
// 
// Written by CoderCow

using System;
using System.IO;

namespace Terraria.Plugins.CoderCow {
  public abstract class MetadataHandlerBase {
    #region [Property: MetadataFilePath]
    private readonly string metadataFilePath;

    protected string MetadataFilePath {
      get { return this.metadataFilePath; }
    }
    #endregion

    #region [Property: Metadata]
    private readonly IMetadataFile metadata;

    public IMetadataFile Metadata {
      get { return this.metadata; }
    }
    #endregion

    #region [Property: PluginTrace]
    private readonly PluginTrace pluginTrace;

    protected PluginTrace PluginTrace {
      get { return this.pluginTrace; }
    }
    #endregion


    #region [Method: Constructor]
    protected MetadataHandlerBase(PluginTrace pluginTrace, string metadataFilePath) {
      this.pluginTrace = pluginTrace;
      this.metadataFilePath = metadataFilePath;

      if (File.Exists(this.metadataFilePath)) {
        try {
          this.metadata = this.ReadMetadataFromFile(this.metadataFilePath);
        } catch (Exception ex) {
          this.PluginTrace.WriteLineError(
            "Reading a metadata file failed. Exception details:\n{0}", ex
          );

          string backupFileName = Path.GetFileNameWithoutExtension(this.metadataFilePath) + ".bak";
          string backupFilePath = Path.Combine(Path.GetDirectoryName(this.metadataFilePath), backupFileName);

          this.metadata = this.ReadMetadataFromFile(backupFilePath);
          this.PluginTrace.WriteLine("Succeeded reading the metadata backup file.");
        }
      } else {
        this.metadata = this.InitMetadata();
        this.WriteMetadata();
      }
    }
    #endregion

    #region [Methods: InitMetadata, ReadMetadataFromFile, WriteMetadata]
    protected abstract IMetadataFile InitMetadata();
    protected abstract IMetadataFile ReadMetadataFromFile(string filePath);

    public virtual void WriteMetadata() {
      // Make a backup of the old file if it exists.
      if (File.Exists(this.MetadataFilePath)) {
        string backupFileName = Path.GetFileNameWithoutExtension(this.MetadataFilePath) + ".bak";
        string backupFilePath = Path.Combine(Path.GetDirectoryName(this.MetadataFilePath), backupFileName);
        File.Copy(this.MetadataFilePath, backupFilePath, true);
      }

      this.Metadata.Write(this.MetadataFilePath);
    }
    #endregion

    #region [Method: ToString]
    public override string ToString() {
      return Path.GetFileName(this.MetadataFilePath);
    }
    #endregion
  }
}
