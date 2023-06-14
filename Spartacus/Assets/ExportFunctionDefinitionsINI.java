/*
 *  This file has been created by using the existing Ghidra ExportFunctionInfoScript.java script as a guide.
 *  One would ask "Why don't you save this output as JSON? Wouldn't that be easier?" And the answer is "yes, it would be",
 *  however I want to keep Spartacus a standalone executable, and adding a NuGet package for JSON would break that.
 */

import java.util.*;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.nio.charset.Charset;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import ghidra.program.model.data.*;

public class ExportFunctionDefinitionsINI extends GhidraScript {

    @Override
    public void run() throws Exception {

        //String iniData = "";
        List<String> iniData = new ArrayList<String>();
        Listing listing = currentProgram.getListing();
        FunctionIterator iter = listing.getFunctions(true);
        while (iter.hasNext() && !monitor.isCancelled()) {
            Function f = iter.next();
            
            iniData.add("[" + f.getName() + "]");
            iniData.add("return=" + f.getReturnType().getName());
            iniData.add("signature=" + f.getSignature().getPrototypeString());
            
            ParameterDefinition[] functionParameters = f.getSignature().getArguments();
            for (int i = 0; i < functionParameters.length; i++) {
                iniData.add("parameters[" + functionParameters[i].getOrdinal() + "]=" + functionParameters[i].getName() + "|" + functionParameters[i].getDataType().getName());
            }
        }
        
        Files.write(Paths.get("%EXPORT_TO%"), iniData, Charset.defaultCharset());
    }
}